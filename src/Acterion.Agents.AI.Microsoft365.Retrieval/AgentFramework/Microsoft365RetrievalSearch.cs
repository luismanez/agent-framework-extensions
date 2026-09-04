using Microsoft.Agents.AI;

namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Adapts Microsoft 365 retrieval hits to Agent Framework text search results.
/// </summary>
public sealed class Microsoft365RetrievalSearch
{
    private readonly IMicrosoft365RetrievalClient retrievalClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="Microsoft365RetrievalSearch"/> class.
    /// </summary>
    /// <param name="retrievalClient">The client used to retrieve SharePoint content.</param>
    public Microsoft365RetrievalSearch(IMicrosoft365RetrievalClient retrievalClient)
    {
        this.retrievalClient = retrievalClient ?? throw new ArgumentNullException(nameof(retrievalClient));
    }

    /// <summary>
    /// Retrieves and maps SharePoint content to Agent Framework search results.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The mapped search results.</returns>
    public async Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Microsoft365RetrievalHit> hits = await this.retrievalClient
            .RetrieveAsync(query, cancellationToken)
            .ConfigureAwait(false);

        return hits.Select(MapHit).ToArray();
    }

    private static TextSearchProvider.TextSearchResult MapHit(Microsoft365RetrievalHit hit)
    {
        ArgumentNullException.ThrowIfNull(hit);

        return new TextSearchProvider.TextSearchResult
        {
            SourceLink = hit.WebUrl,
            SourceName = GetSourceName(hit),
            Text = string.Join(
                "\n",
                hit.Extracts
                    .Select(extract => extract.Text)
                    .Where(text => !string.IsNullOrWhiteSpace(text))),
            RawRepresentation = hit,
        };
    }

    private static string GetSourceName(Microsoft365RetrievalHit hit)
    {
        if (hit.ResourceMetadata.TryGetValue("title", out System.Text.Json.JsonElement title) &&
            title.ValueKind == System.Text.Json.JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(title.GetString()))
        {
            return title.GetString()!;
        }

        if (Uri.TryCreate(hit.WebUrl, UriKind.Absolute, out Uri? uri))
        {
            string segment = uri.Segments.LastOrDefault(segment => segment != "/")?.TrimEnd('/') ?? string.Empty;
            if (!string.IsNullOrEmpty(segment))
            {
                return Uri.UnescapeDataString(segment);
            }
        }

        return hit.WebUrl;
    }
}