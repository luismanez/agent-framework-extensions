namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Represents a relevant text extract from a retrieval result.
/// </summary>
public sealed class Microsoft365RetrievalExtract
{
    internal Microsoft365RetrievalExtract(string text, double? relevanceScore)
    {
        ArgumentNullException.ThrowIfNull(text);

        Text = text;
        RelevanceScore = relevanceScore;
    }

    /// <summary>
    /// Gets the extracted text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the optional relevance score.
    /// </summary>
    public double? RelevanceScore { get; }
}