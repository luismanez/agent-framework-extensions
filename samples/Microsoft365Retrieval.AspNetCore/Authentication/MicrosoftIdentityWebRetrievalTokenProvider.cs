using System.Security.Claims;
using Acterion.Agents.AI.Microsoft365.Retrieval;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;

namespace Microsoft365Retrieval.AspNetCore.Authentication;

internal sealed class MicrosoftIdentityWebRetrievalTokenProvider(
    IHttpContextAccessor httpContextAccessor)
    : IMicrosoft365RetrievalTokenProvider
{
    private static readonly string[] GraphScopes = ["https://graph.microsoft.com/.default"];

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        HttpContext httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("A delegated user is required to acquire a Microsoft Graph token.");
        ClaimsPrincipal user = httpContext.User
            ?? throw new InvalidOperationException("A delegated user is required to acquire a Microsoft Graph token.");
        ITokenAcquisition tokenAcquisition = httpContext.RequestServices.GetRequiredService<ITokenAcquisition>();

        return await tokenAcquisition
            .GetAccessTokenForUserAsync(GraphScopes, user: user)
            .WaitAsync(cancellationToken);
    }
}