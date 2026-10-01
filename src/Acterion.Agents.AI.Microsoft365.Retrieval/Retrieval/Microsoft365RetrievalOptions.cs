namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Configures requests to the Microsoft 365 Copilot Retrieval API.
/// </summary>
public sealed class Microsoft365RetrievalOptions
{
    /// <summary>
    /// Gets or sets the data source used by this client. Defaults to SharePoint.
    /// </summary>
    public Microsoft365RetrievalDataSource DataSource { get; set; } =
        Microsoft365RetrievalDataSource.SharePoint;

    /// <summary>
    /// Gets or sets the optional external connection IDs used when the source is ExternalItem.
    /// </summary>
    public IReadOnlyCollection<string>? ExternalItemConnectionIds { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retrieval results, from 1 through 25.
    /// </summary>
    public int MaximumNumberOfResults { get; set; } = 8;

    /// <summary>
    /// Gets or sets the optional non-whitespace KQL filter expression for the selected source.
    /// </summary>
    public string? FilterExpression { get; set; }

    /// <summary>
    /// Gets or sets the metadata field names requested for each result. An empty collection requests none.
    /// </summary>
    public IReadOnlyCollection<string> ResourceMetadata { get; set; } = ["title", "author"];
}
