using System.Collections.ObjectModel;
using System.Text.Json;

namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Represents a SharePoint retrieval result.
/// </summary>
public sealed class Microsoft365RetrievalHit
{
    internal Microsoft365RetrievalHit(
        string webUrl,
        IReadOnlyList<Microsoft365RetrievalExtract> extracts,
        string? resourceType,
        IReadOnlyDictionary<string, JsonElement> resourceMetadata)
    {
        ArgumentNullException.ThrowIfNull(webUrl);
        ArgumentNullException.ThrowIfNull(extracts);
        ArgumentNullException.ThrowIfNull(resourceMetadata);

        Dictionary<string, JsonElement> metadata = new(StringComparer.Ordinal);
        foreach ((string name, JsonElement value) in resourceMetadata)
        {
            if (value.ValueKind is JsonValueKind.Array or JsonValueKind.Object or JsonValueKind.Undefined)
            {
                throw new ArgumentException(
                    "Resource metadata values must be JSON scalars.",
                    nameof(resourceMetadata));
            }

            metadata.Add(name, value.Clone());
        }

        WebUrl = webUrl;
        Extracts = new ReadOnlyCollection<Microsoft365RetrievalExtract>(extracts.ToArray());
        ResourceType = resourceType;
        ResourceMetadata = new ReadOnlyDictionary<string, JsonElement>(metadata);
    }

    /// <summary>
    /// Gets the original web URL returned for the result.
    /// </summary>
    public string WebUrl { get; }

    /// <summary>
    /// Gets the relevant extracts in response order.
    /// </summary>
    public IReadOnlyList<Microsoft365RetrievalExtract> Extracts { get; }

    /// <summary>
    /// Gets the optional resource type.
    /// </summary>
    public string? ResourceType { get; }

    /// <summary>
    /// Gets the requested resource metadata as JSON scalar values.
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement> ResourceMetadata { get; }
}