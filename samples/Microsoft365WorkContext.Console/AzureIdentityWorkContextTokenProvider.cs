using Acterion.Agents.AI.Microsoft365.WorkContext;
using Azure.Core;

internal sealed class AzureIdentityWorkContextTokenProvider(
    TokenCredential credential,
    TokenRequestContext graphTokenRequest)
    : IMicrosoft365WorkContextTokenProvider
{
    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        AccessToken accessToken = await credential.GetTokenAsync(graphTokenRequest, cancellationToken);
        return accessToken.Token;
    }
}
