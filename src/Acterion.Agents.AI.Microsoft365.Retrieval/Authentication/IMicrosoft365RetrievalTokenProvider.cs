namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Supplies a host-acquired delegated Microsoft Graph access token for a retrieval operation.
/// </summary>
public interface IMicrosoft365RetrievalTokenProvider
{
    /// <summary>
    /// Gets the delegated access token for the current operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The delegated Microsoft Graph access token.</returns>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}