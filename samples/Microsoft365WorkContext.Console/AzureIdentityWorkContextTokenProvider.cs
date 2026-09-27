using Acterion.Agents.AI.Microsoft365.WorkContext;
using Azure.Core;

internal sealed class AzureIdentityWorkContextTokenProvider(TokenCredential credential)
    : IMicrosoft365WorkContextTokenProvider
{
    private static readonly TokenRequestContext GraphTokenRequest = new(["https://graph.microsoft.com/.default"]);

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        AccessToken accessToken = await credential.GetTokenAsync(GraphTokenRequest, cancellationToken);
        return accessToken.Token;
    }
}
