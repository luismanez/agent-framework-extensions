using System.Net;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class Microsoft365WorkContextDependencyInjectionTests
{
    [Fact]
    public void AddMicrosoft365WorkContext_RejectsNullArgumentsSynchronously()
    {
        ServiceCollection services = new();

        Assert.Throws<ArgumentNullException>(() =>
            Microsoft365WorkContextServiceCollectionExtensions.AddMicrosoft365WorkContext(
                null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => services.AddMicrosoft365WorkContext(null!));
    }

    [Fact]
    public void Resolution_RequiresHostTokenProvider()
    {
        ServiceCollection services = new();
        services.AddMicrosoft365WorkContext(_ => { });
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<IMicrosoft365WorkContextClient>());

        Assert.Contains(nameof(IMicrosoft365WorkContextTokenProvider), exception.Message);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IMicrosoft365WorkContextTokenProvider));
    }

    [Fact]
    public async Task Registration_UsesGraphBaseAddressTransientClientHostClockAndOptionSnapshot()
    {
        DateTimeOffset now = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
        FixedTimeProvider clock = new(now);
        StaticTokenProvider tokenProvider = new();
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton<IMicrosoft365WorkContextTokenProvider>(tokenProvider);
        services.AddMicrosoft365WorkContext(options =>
        {
            options.EnableUserProfile = false;
            options.EnableManager = false;
            options.EnableWorkSettings = false;
            options.EnableCalendar = false;
        });
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        IMicrosoft365WorkContextClient first = provider.GetRequiredService<IMicrosoft365WorkContextClient>();
        IMicrosoft365WorkContextClient second = provider.GetRequiredService<IMicrosoft365WorkContextClient>();
        provider.GetRequiredService<IOptions<Microsoft365WorkContextOptions>>().Value.EnableUserProfile = true;
        WorkContextSnapshot snapshot = await first.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.NotSame(first, second);
        Assert.Same(clock, provider.GetRequiredService<TimeProvider>());
        Assert.Equal(now, snapshot.CapturedAtUtc);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.UserProfile.Status);
        Assert.Equal(0, tokenProvider.CallCount);
        Assert.Equal(
            new Uri("https://graph.microsoft.com/"),
            provider.GetRequiredService<IHttpClientFactory>()
                .CreateClient(nameof(Microsoft365WorkContextClient)).BaseAddress);
    }

    [Fact]
    public void Registration_UsesSystemClockFallbackWithoutReplacingOtherServices()
    {
        ServiceCollection services = new();
        object marker = new();
        services.AddSingleton(marker);
        services.AddSingleton<IMicrosoft365WorkContextTokenProvider, StaticTokenProvider>();
        services.AddHttpClient("other", client => client.BaseAddress = new Uri("https://example.invalid/"));
        services.AddMicrosoft365WorkContext(_ => { });
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
        Assert.Same(marker, provider.GetRequiredService<object>());
        Assert.Equal(new Uri("https://example.invalid/"),
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("other").BaseAddress);
        Assert.IsType<StaticTokenProvider>(provider.GetRequiredService<IMicrosoft365WorkContextTokenProvider>());
    }

    [Fact]
    public void Resolution_RejectsInvalidOptions()
    {
        ServiceCollection services = new();
        services.AddSingleton<IMicrosoft365WorkContextTokenProvider, StaticTokenProvider>();
        services.AddMicrosoft365WorkContext(options => options.MaximumCalendarEvents = 26);
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IMicrosoft365WorkContextClient>());

        Assert.Contains(nameof(Microsoft365WorkContextOptions.MaximumCalendarEvents), exception.Message);
    }

    [Fact]
    public async Task ScopedProviders_KeepTokensAndFreshSnapshotsIsolatedAcrossUsers()
    {
        int nextUser = 0;
        UserResponseHandler handler = new();
        ServiceCollection services = new();
        services.AddScoped(_ => new ScopeUser(Interlocked.Increment(ref nextUser) == 1 ? "token-a" : "token-b"));
        services.AddScoped<IMicrosoft365WorkContextTokenProvider>(provider =>
            new ScopedTokenProvider(provider.GetRequiredService<ScopeUser>()));
        services.AddMicrosoft365WorkContext(_ => { });
        services.AddHttpClient(nameof(Microsoft365WorkContextClient))
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        using IServiceScope scopeA = provider.CreateScope();
        using IServiceScope scopeB = provider.CreateScope();

        IMicrosoft365WorkContextClient clientA = scopeA.ServiceProvider
            .GetRequiredService<IMicrosoft365WorkContextClient>();
        IMicrosoft365WorkContextClient clientB = scopeB.ServiceProvider
            .GetRequiredService<IMicrosoft365WorkContextClient>();
        WorkContextSnapshot firstA = await clientA.GetSnapshotAsync(TestContext.Current.CancellationToken);
        WorkContextSnapshot firstB = await clientB.GetSnapshotAsync(TestContext.Current.CancellationToken);
        WorkContextSnapshot secondA = await clientA.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.NotSame(clientA, clientB);
        Assert.NotSame(clientA, scopeA.ServiceProvider.GetRequiredService<IMicrosoft365WorkContextClient>());
        Assert.Equal("token-a-profile-1", firstA.UserProfile.Value?.DisplayName);
        Assert.Equal("token-b-profile-1", firstB.UserProfile.Value?.DisplayName);
        Assert.Equal("token-a-profile-2", secondA.UserProfile.Value?.DisplayName);
        Assert.NotSame(firstA, secondA);
        Assert.Equal(["token-a", "token-b", "token-a"], handler.ObservedTokens);
    }

    [Fact]
    public void ClientFields_ContainOnlyStatelessDependenciesAndImmutableOptions()
    {
        FieldInfo[] fields = typeof(Microsoft365WorkContextClient).GetFields(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

        Assert.Equal(
            new[]
            {
                typeof(HttpClient),
                typeof(IMicrosoft365WorkContextTokenProvider),
                typeof(ILogger<Microsoft365WorkContextClient>),
                typeof(Microsoft365WorkContextOptionsSnapshot),
                typeof(TimeProvider),
            }.OrderBy(type => type.FullName),
            fields.Select(field => field.FieldType).OrderBy(type => type.FullName));
        Assert.All(fields, field => Assert.True(field.IsInitOnly));
    }

    private sealed class StaticTokenProvider : IMicrosoft365WorkContextTokenProvider
    {
        public int CallCount { get; private set; }

        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            this.CallCount++;
            return Task.FromResult("token");
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed record ScopeUser(string Token);

    private sealed class ScopedTokenProvider(ScopeUser user) : IMicrosoft365WorkContextTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(user.Token);
    }

    private sealed class UserResponseHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, int> callsByToken = new(StringComparer.Ordinal);

        public List<string> ObservedTokens { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string token = request.Headers.Authorization?.Parameter
                ?? throw new InvalidOperationException("Missing delegated token.");
            this.ObservedTokens.Add(token);
            this.callsByToken.TryGetValue(token, out int count);
            this.callsByToken[token] = ++count;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""{ "displayName": "{{token}}-profile-{{count}}" }""",
                    Encoding.UTF8,
                    "application/json"),
            });
        }
    }
}
