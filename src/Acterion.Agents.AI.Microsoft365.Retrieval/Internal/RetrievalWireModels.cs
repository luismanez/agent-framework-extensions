using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acterion.Agents.AI.Microsoft365.Retrieval;

internal sealed class RetrievalApiRequest
{
    [JsonPropertyName("queryString")]
    public required string QueryString { get; init; }

    [JsonPropertyName("dataSource")]
    public string DataSource { get; init; } = "sharePoint";

    [JsonPropertyName("filterExpression")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FilterExpression { get; init; }

    [JsonPropertyName("resourceMetadata")]
    public required IReadOnlyCollection<string> ResourceMetadata { get; init; }

    [JsonPropertyName("maximumNumberOfResults")]
    public required int MaximumNumberOfResults { get; init; }
}

internal sealed class RetrievalApiResponse
{
    [JsonPropertyName("retrievalHits")]
    public IReadOnlyList<RetrievalApiHit>? RetrievalHits { get; init; }
}

internal sealed class RetrievalApiHit
{
    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; init; }

    [JsonPropertyName("extracts")]
    public IReadOnlyList<RetrievalApiExtract>? Extracts { get; init; }

    [JsonPropertyName("resourceType")]
    public string? ResourceType { get; init; }

    [JsonPropertyName("resourceMetadata")]
    public IReadOnlyDictionary<string, JsonElement>? ResourceMetadata { get; init; }

    [JsonPropertyName("sensitivityLabel")]
    public RetrievalApiSensitivityLabel? SensitivityLabel { get; init; }
}

internal sealed class RetrievalApiExtract
{
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    [JsonPropertyName("relevanceScore")]
    public double? RelevanceScore { get; init; }
}

internal sealed class RetrievalApiSensitivityLabel
{
    [JsonPropertyName("sensitivityLabelId")]
    public string? SensitivityLabelId { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("toolTip")]
    public string? ToolTip { get; init; }

    [JsonPropertyName("priority")]
    public int? Priority { get; init; }

    [JsonPropertyName("color")]
    public string? Color { get; init; }
}
