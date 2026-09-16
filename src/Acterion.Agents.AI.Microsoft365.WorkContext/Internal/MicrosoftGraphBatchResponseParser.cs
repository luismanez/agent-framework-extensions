using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal static class MicrosoftGraphBatchResponseParser
{
    internal static async Task<IReadOnlyList<MicrosoftGraphBatchSubresponse>> ParseAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        await using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        MicrosoftGraphBatchResponse? response = await JsonSerializer
            .DeserializeAsync<MicrosoftGraphBatchResponse>(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (response?.Responses is null)
        {
            throw new InvalidDataException("The Microsoft Graph batch response envelope is invalid.");
        }

        return Array.AsReadOnly(response.Responses.ToArray());
    }
}

internal sealed record MicrosoftGraphBatchResponse(
    [property: JsonPropertyName("responses")]
    IReadOnlyList<MicrosoftGraphBatchSubresponse>? Responses);

internal sealed record MicrosoftGraphBatchSubresponse(
    [property: JsonPropertyName("id")]
    string? Id,
    [property: JsonPropertyName("status")]
    int Status,
    [property: JsonPropertyName("headers")]
    IReadOnlyDictionary<string, string>? Headers,
    [property: JsonPropertyName("body")]
    JsonElement Body);