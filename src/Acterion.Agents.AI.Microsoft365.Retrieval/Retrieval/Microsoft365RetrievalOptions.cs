namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Configures requests to the Microsoft 365 Copilot Retrieval API.
/// </summary>
public sealed class Microsoft365RetrievalOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retrieval results.
    /// </summary>
    public int MaximumNumberOfResults { get; set; } = 8;

    /// <summary>
    /// Gets or sets the optional SharePoint filter expression.
    /// </summary>
    public string? FilterExpression { get; set; }

    /// <summary>
    /// Gets or sets the metadata fields requested for each result.
    /// </summary>
    public IReadOnlyCollection<string> ResourceMetadata { get; set; } = ["title", "author"];
}