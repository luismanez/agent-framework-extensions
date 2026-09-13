using Microsoft.Extensions.Options;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class Microsoft365WorkContextOptionsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(26)]
    public void Client_RejectsInvalidMaximumCalendarEvents(int maximumCalendarEvents)
    {
        Microsoft365WorkContextOptions options = AllDisabledOptions();
        options.MaximumCalendarEvents = maximumCalendarEvents;

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => CreateClient(options));

        Assert.Contains("MaximumCalendarEvents", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(169)]
    public void Client_RejectsInvalidCalendarLookAheadEvenWhenCalendarIsDisabled(int hours)
    {
        Microsoft365WorkContextOptions options = AllDisabledOptions();
        options.CalendarLookAhead = TimeSpan.FromHours(hours);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => CreateClient(options));

        Assert.Contains("CalendarLookAhead", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Client_RejectsUndefinedErrorBehavior()
    {
        Microsoft365WorkContextOptions options = AllDisabledOptions();
        options.ErrorBehavior = (WorkContextErrorBehavior)int.MaxValue;

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => CreateClient(options));

        Assert.Contains("ErrorBehavior", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenAllFacetsDisabled_ReturnsDisabledWithoutExternalWork()
    {
        CountingTokenProvider tokenProvider = new();
        CountingHttpMessageHandler handler = new();
        FixedTimeProvider timeProvider = new(DateTimeOffset.Parse("2026-09-13T12:34:56Z"));
        Microsoft365WorkContextOptions options = AllDisabledOptions();
        Microsoft365WorkContextClient client = CreateClient(options, tokenProvider, handler, timeProvider);
        options.EnableUserProfile = true;

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(timeProvider.GetUtcNow(), snapshot.CapturedAtUtc);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.UserProfile.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Manager.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.WorkSettings.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Calendar.Status);
        Assert.Equal(0, tokenProvider.CallCount);
        Assert.Equal(0, handler.RequestCount);
    }

    private static Microsoft365WorkContextOptions AllDisabledOptions() => new()
    {
        EnableUserProfile = false,
        EnableManager = false,
        EnableWorkSettings = false,
        EnableCalendar = false,
    };

    private static Microsoft365WorkContextClient CreateClient(
        Microsoft365WorkContextOptions options,
        CountingTokenProvider? tokenProvider = null,
        CountingHttpMessageHandler? handler = null,
        TimeProvider? timeProvider = null) =>
        new(
            new HttpClient(handler ?? new CountingHttpMessageHandler())
            {
                BaseAddress = new Uri("https://graph.microsoft.com/"),
            },
            tokenProvider ?? new CountingTokenProvider(),
            options,
            timeProvider ?? TimeProvider.System);

    private sealed class CountingTokenProvider : IMicrosoft365WorkContextTokenProvider
    {
        public int CallCount { get; private set; }

        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult("token");
        }
    }

    private sealed class CountingHttpMessageHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}