using System.Text.Json.Serialization;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal sealed record MicrosoftGraphBatchRequest(
    [property: JsonPropertyName("requests")]
    IReadOnlyList<MicrosoftGraphBatchSubrequest> Requests);

internal sealed record MicrosoftGraphBatchSubrequest(
    [property: JsonPropertyName("id")]
    string Id,
    [property: JsonPropertyName("method")]
    string Method,
    [property: JsonPropertyName("url")]
    string Url);