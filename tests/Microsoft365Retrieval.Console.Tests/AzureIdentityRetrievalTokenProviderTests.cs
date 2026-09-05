using Azure.Core;
using Xunit;

namespace Microsoft365Retrieval.Console.Tests;

public sealed class AzureIdentityRetrievalTokenProviderTests
{
    [Fact]
    public async Task GetAccessTokenAsync_RequestsTheGraphDefaultScopeAndForwardsCancellation()
    {
        RecordingTokenCredential credential = new();
        AzureIdentityRetrievalTokenProvider provider = new(credential);
        using CancellationTokenSource cancellationSource = new();

        string token = await provider.GetAccessTokenAsync(cancellationSource.Token);

        Assert.Equal("delegated-token", token);
        Assert.Equal(["https://graph.microsoft.com/.default"], credential.LastScopes!);
        Assert.Equal(cancellationSource.Token, credential.LastCancellationToken);
    }

    private sealed class RecordingTokenCredential : TokenCredential
    {
        public string[]? LastScopes { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public override AccessToken GetToken(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            LastScopes = requestContext.Scopes;
            LastCancellationToken = cancellationToken;

            return ValueTask.FromResult(new AccessToken("delegated-token", DateTimeOffset.MaxValue));
        }
    }
}

public sealed class SampleConfigurationTests
{
    [Fact]
    public void FromEnvironment_ReportsEveryMissingRequiredSetting()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => SampleConfiguration.FromEnvironment(_ => null));

        Assert.Contains("AZURE_TENANT_ID", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AZURE_CLIENT_ID", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AZURE_OPENAI_ENDPOINT", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AZURE_OPENAI_DEPLOYMENT_NAME", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromEnvironment_LoadsTheOptionalFilter()
    {
        SampleConfiguration configuration = SampleConfiguration.FromEnvironment(settingName =>
            settingName == "MICROSOFT365_RETRIEVAL_FILTER" ? "path ne '/private'" : "configured-value");

        Assert.Equal("path ne '/private'", configuration.RetrievalFilter);
    }
}