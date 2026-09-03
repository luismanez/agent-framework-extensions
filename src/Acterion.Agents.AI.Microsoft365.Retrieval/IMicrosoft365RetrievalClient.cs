namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Retrieves permission-trimmed SharePoint content through Microsoft Graph.
/// </summary>
public interface IMicrosoft365RetrievalClient
{
    /// <summary>
    /// Retrieves SharePoint content relevant to a query.
    /// </summary>
    /// <param name="query">The query submitted to the retrieval API.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The retrieval hits in response order.</returns>
    Task<IReadOnlyList<Microsoft365RetrievalHit>> RetrieveAsync(
        string query,
        CancellationToken cancellationToken = default);
}