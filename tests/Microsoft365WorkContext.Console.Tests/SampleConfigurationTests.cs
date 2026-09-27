using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft365WorkContext.Console.Tests;

public sealed class SampleConfigurationTests
{
    [Fact]
    public void SnapshotOnlyMode_DoesNotRequireModelSettings()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["MicrosoftEntra:TenantId"] = "tenant-id",
            ["MicrosoftEntra:ClientId"] = "client-id",
        });

        SampleConfiguration result = SampleConfiguration.FromConfiguration(configuration, requireAzureOpenAI: false);

        Assert.Null(result.AzureOpenAIEndpoint);
        Assert.Null(result.AzureOpenAIDeploymentName);
        Assert.True(result.EnableUserProfile);
        Assert.True(result.EnableManager);
        Assert.True(result.EnableWorkSettings);
        Assert.True(result.EnableCalendar);
        Assert.Equal(5, result.MaximumCalendarEvents);
    }

    [Fact]
    public void AgentMode_RequiresBothIdentityAndModelSettings()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SampleConfiguration.FromConfiguration(CreateConfiguration([])));

        Assert.Contains("MicrosoftEntra:TenantId", exception.Message);
        Assert.Contains("MicrosoftEntra:ClientId", exception.Message);
        Assert.Contains("AzureOpenAI:TenantId", exception.Message);
        Assert.Contains("AzureOpenAI:Endpoint", exception.Message);
        Assert.Contains("AzureOpenAI:DeploymentName", exception.Message);
    }

    [Fact]
    public void Configuration_RejectsNonHttpsModelEndpoint()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["MicrosoftEntra:TenantId"] = "tenant-id",
            ["MicrosoftEntra:ClientId"] = "client-id",
            ["AzureOpenAI:TenantId"] = "model-tenant-id",
            ["AzureOpenAI:Endpoint"] = "http://example.invalid/",
            ["AzureOpenAI:DeploymentName"] = "chat",
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SampleConfiguration.FromConfiguration(configuration));

        Assert.Contains("absolute HTTPS URI", exception.Message);
    }

    [Fact]
    public void Configuration_ReadsEnabledFacets()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["MicrosoftEntra:TenantId"] = "tenant-id",
            ["MicrosoftEntra:ClientId"] = "client-id",
            ["Microsoft365WorkContext:EnableManager"] = "false",
            ["Microsoft365WorkContext:EnableCalendar"] = "false",
            ["Microsoft365WorkContext:MaximumCalendarEvents"] = "8",
        });

        SampleConfiguration result = SampleConfiguration.FromConfiguration(configuration, requireAzureOpenAI: false);

        Assert.False(result.EnableManager);
        Assert.False(result.EnableCalendar);
        Assert.True(result.EnableUserProfile);
        Assert.True(result.EnableWorkSettings);
        Assert.Equal(8, result.MaximumCalendarEvents);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("26")]
    public void Configuration_RejectsInvalidMaximumCalendarEvents(string value)
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["MicrosoftEntra:TenantId"] = "tenant-id",
            ["MicrosoftEntra:ClientId"] = "client-id",
            ["Microsoft365WorkContext:MaximumCalendarEvents"] = value,
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SampleConfiguration.FromConfiguration(configuration, requireAzureOpenAI: false));

        Assert.Contains("MaximumCalendarEvents", exception.Message);
    }

    [Theory]
    [InlineData("--snapshot-only", true)]
    [InlineData("--SNAPSHOT-ONLY", true)]
    [InlineData("snapshot-only", false)]
    public void CommandLine_RecognizesSnapshotOnly(string argument, bool expected) =>
        Assert.Equal(expected, SampleCommandLine.IsSnapshotOnly([argument]));

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}

public sealed class AzureIdentityWorkContextTokenProviderTests
{
    [Fact]
    public async Task Provider_RequestsGraphDelegatedTokenAndForwardsCancellation()
    {
        RecordingCredential credential = new();
        AzureIdentityWorkContextTokenProvider provider = new(credential);
        using CancellationTokenSource cancellation = new();

        string token = await provider.GetAccessTokenAsync(cancellation.Token);

        Assert.Equal("delegated-token", token);
        Assert.NotNull(credential.Scopes);
        Assert.Equal(["https://graph.microsoft.com/.default"], credential.Scopes);
        Assert.Equal(cancellation.Token, credential.CancellationToken);
    }

    [Fact]
    public void DeviceCredential_UsesItsOwnEncryptedCache()
    {
        DeviceCodeCredentialOptions options = PersistentDeviceCodeCredential.CreateOptions(
            "tenant-id", "client-id", null, (_, _) => Task.CompletedTask);

        Assert.Equal("Acterion.Microsoft365WorkContext.Console", options.TokenCachePersistenceOptions.Name);
        Assert.False(options.TokenCachePersistenceOptions.UnsafeAllowUnencryptedStorage);
    }

    private sealed class RecordingCredential : TokenCredential
    {
        public string[]? Scopes { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            Scopes = requestContext.Scopes;
            CancellationToken = cancellationToken;
            return ValueTask.FromResult(new AccessToken("delegated-token", DateTimeOffset.MaxValue));
        }
    }
}
