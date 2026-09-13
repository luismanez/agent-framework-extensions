namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Provides a delegated Microsoft Graph access token for the current invocation.</summary>
public interface IMicrosoft365WorkContextTokenProvider
{
    /// <summary>Gets a delegated Microsoft Graph access token for the current invocation.</summary>
    /// <param name="cancellationToken">The token used to cancel token acquisition.</param>
    /// <returns>The delegated Microsoft Graph bearer token.</returns>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}