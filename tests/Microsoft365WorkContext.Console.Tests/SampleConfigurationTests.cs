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
        Assert.Null(result.AzureOpenAITenantId);
        Assert.True(result.EnableUserProfile);
        Assert.True(result.EnableManager);
        Assert.True(result.EnableWorkSettings);
        Assert.True(result.EnableCalendar);
        Assert.Equal(5, result.MaximumCalendarEvents);
        Assert.Equal(
            [
                "https://graph.microsoft.com/User.Read",
                "https://graph.microsoft.com/User.Read.All",
                "https://graph.microsoft.com/MailboxSettings.Read",
                "https://graph.microsoft.com/Calendars.ReadBasic",
            ],
            result.GetGraphScopes());
    }

    [Fact]
    public void AgentMode_RequiresBothIdentityAndModelSettings()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SampleConfiguration.FromConfiguration(CreateConfiguration([])));

        Assert.Contains("MicrosoftEntra:TenantId", exception.Message);
        Assert.Contains("MicrosoftEntra:ClientId", exception.Message);
        Assert.Contains("AzureOpenAI:Endpoint", exception.Message);
        Assert.Contains("AzureOpenAI:DeploymentName", exception.Message);
    }

    [Fact]
    public void AgentMode_AcceptsModelSettingsWithoutTenantId()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["MicrosoftEntra:TenantId"] = "graph-tenant-id",
            ["MicrosoftEntra:ClientId"] = "client-id",
            ["AzureOpenAI:Endpoint"] = "https://example.openai.azure.com/",
            ["AzureOpenAI:DeploymentName"] = "chat",
        });

        SampleConfiguration result = SampleConfiguration.FromConfiguration(configuration);

        Assert.Equal(new Uri("https://example.openai.azure.com/"), result.AzureOpenAIEndpoint);
        Assert.Equal("chat", result.AzureOpenAIDeploymentName);
        Assert.Null(result.AzureOpenAITenantId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("model-tenant-id")]
    public void AgentMode_UsesTenantIdOnlyWhenConfigured(string tenantId)
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["MicrosoftEntra:TenantId"] = "graph-tenant-id",
            ["MicrosoftEntra:ClientId"] = "client-id",
            ["AzureOpenAI:TenantId"] = tenantId,
            ["AzureOpenAI:Endpoint"] = "https://example.openai.azure.com/",
            ["AzureOpenAI:DeploymentName"] = "chat",
        });

        SampleConfiguration result = SampleConfiguration.FromConfiguration(configuration);

        Assert.Equal(string.IsNullOrWhiteSpace(tenantId) ? null : tenantId, result.AzureOpenAITenantId);
    }

    [Fact]
    public void Configuration_RejectsNonHttpsModelEndpoint()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["MicrosoftEntra:TenantId"] = "tenant-id",
            ["MicrosoftEntra:ClientId"] = "client-id",
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
        Assert.Equal(
            ["https://graph.microsoft.com/User.Read", "https://graph.microsoft.com/MailboxSettings.Read"],
            result.GetGraphScopes());
    }

    [Fact]
    public void Configuration_RejectsAllFacetsDisabled()
    {
        IConfiguration configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["MicrosoftEntra:TenantId"] = "tenant-id",
            ["MicrosoftEntra:ClientId"] = "client-id",
            ["Microsoft365WorkContext:EnableUserProfile"] = "false",
            ["Microsoft365WorkContext:EnableManager"] = "false",
            ["Microsoft365WorkContext:EnableWorkSettings"] = "false",
            ["Microsoft365WorkContext:EnableCalendar"] = "false",
        });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SampleConfiguration.FromConfiguration(configuration, requireAzureOpenAI: false));

        Assert.Contains("Enable at least one", exception.Message);
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
        TokenRequestContext request = new(
            ["https://graph.microsoft.com/User.Read.All", "https://graph.microsoft.com/MailboxSettings.Read"]);
        AzureIdentityWorkContextTokenProvider provider = new(credential, request);
        using CancellationTokenSource cancellation = new();

        string token = await provider.GetAccessTokenAsync(cancellation.Token);

        Assert.Equal("delegated-token", token);
        Assert.NotNull(credential.Scopes);
        Assert.Equal(
            ["https://graph.microsoft.com/User.Read.All", "https://graph.microsoft.com/MailboxSettings.Read"],
            credential.Scopes);
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
