using Acterion.Agents.AI.Microsoft365.Retrieval;
using Azure.Core;

internal sealed class AzureIdentityRetrievalTokenProvider(TokenCredential credential) : IMicrosoft365RetrievalTokenProvider
{
    private static readonly TokenRequestContext GraphTokenRequest = new(["https://graph.microsoft.com/.default"]);

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        AccessToken accessToken = await credential.GetTokenAsync(GraphTokenRequest, cancellationToken);
        return accessToken.Token;
    }
}