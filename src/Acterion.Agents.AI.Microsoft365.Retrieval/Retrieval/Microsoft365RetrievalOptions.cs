namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Configures requests to the Microsoft 365 Copilot Retrieval API.
/// </summary>
public sealed class Microsoft365RetrievalOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retrieval results, from 1 through 25.
    /// </summary>
    public int MaximumNumberOfResults { get; set; } = 8;

    /// <summary>
    /// Gets or sets the optional non-whitespace SharePoint KQL filter expression.
    /// </summary>
    public string? FilterExpression { get; set; }

    /// <summary>
    /// Gets or sets the nonempty metadata field names requested for each result.
    /// </summary>
    public IReadOnlyCollection<string> ResourceMetadata { get; set; } = ["title", "author"];
}