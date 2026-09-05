using System.Security.Claims;
using Acterion.Agents.AI.Microsoft365.Retrieval;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;

namespace Microsoft365Retrieval.AspNetCore.Authentication;

internal sealed class MicrosoftIdentityWebRetrievalTokenProvider(
    ITokenAcquisition tokenAcquisition,
    IHttpContextAccessor httpContextAccessor)
    : IMicrosoft365RetrievalTokenProvider
{
    private static readonly string[] GraphScopes = ["https://graph.microsoft.com/.default"];

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ClaimsPrincipal user = httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException("A delegated user is required to acquire a Microsoft Graph token.");

        return await tokenAcquisition
            .GetAccessTokenForUserAsync(GraphScopes, user: user)
            .WaitAsync(cancellationToken);
    }
}