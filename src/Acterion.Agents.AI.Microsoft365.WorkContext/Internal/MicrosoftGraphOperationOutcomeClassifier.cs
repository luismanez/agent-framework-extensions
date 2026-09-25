using System.Net;
using System.Text.Json;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal static class MicrosoftGraphOperationOutcomeClassifier
{
    internal static MicrosoftGraphOperationOutcome Classify(
        MicrosoftGraphCorrelatedResponse correlatedResponse)
    {
        ArgumentNullException.ThrowIfNull(correlatedResponse);

        if (!correlatedResponse.IsValid || correlatedResponse.Response is null)
        {
            return Failed(
                correlatedResponse.Operation,
                WorkContextFailureKind.InvalidResponse,
                statusCode: null,
                requestId: null);
        }

        MicrosoftGraphBatchSubresponse response = correlatedResponse.Response;
        return Classify(
            correlatedResponse.Operation,
            response.Status,
            response.Headers,
            response.Body);
    }

    internal static MicrosoftGraphOperationOutcome Classify(
        MicrosoftGraphOperation operation,
        int status,
        IReadOnlyDictionary<string, string>? headers,
        JsonElement body)
    {
        ArgumentNullException.ThrowIfNull(operation);

        string? requestId = GetRequestId(headers);
        if (status is < 100 or > 599)
        {
            return Failed(
                operation,
                WorkContextFailureKind.InvalidResponse,
                statusCode: null,
                requestId);
        }

        HttpStatusCode statusCode = (HttpStatusCode)status;
        if (status == (int)HttpStatusCode.NotFound &&
            operation.Id == "manager" &&
            operation.Facet == WorkContextFacet.Manager)
        {
            return MicrosoftGraphOperationOutcome.Unavailable(operation);
        }

        if (status == (int)HttpStatusCode.NoContent &&
            operation.Facet == WorkContextFacet.WorkSettings)
        {
            return MicrosoftGraphOperationOutcome.Unavailable(operation);
        }

        if (status is >= 200 and <= 299)
        {
            bool hasValidPayloadShape = operation.Id == "work-time-zone"
                ? body.ValueKind == JsonValueKind.String
                : body.ValueKind == JsonValueKind.Object;

            return hasValidPayloadShape
                ? MicrosoftGraphOperationOutcome.Succeeded(operation, body)
                : Failed(operation, WorkContextFailureKind.InvalidResponse, statusCode, requestId);
        }

        WorkContextFailureKind failureKind = ClassifyFailureKind(statusCode);

        return Failed(operation, failureKind, statusCode, requestId);
    }

    internal static WorkContextFailureKind ClassifyFailureKind(HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.Unauthorized => WorkContextFailureKind.Authentication,
            HttpStatusCode.Forbidden => WorkContextFailureKind.Authorization,
            HttpStatusCode.TooManyRequests => WorkContextFailureKind.Throttled,
            _ => WorkContextFailureKind.Service,
        };

    internal static string? SelectRequestId(string? requestId, string? clientRequestId) =>
        NormalizeRequestId(requestId) ?? NormalizeRequestId(clientRequestId);

    private static MicrosoftGraphOperationOutcome Failed(
        MicrosoftGraphOperation operation,
        WorkContextFailureKind failureKind,
        HttpStatusCode? statusCode,
        string? requestId) =>
        MicrosoftGraphOperationOutcome.Failed(
            operation,
            new WorkContextFacetFailure(failureKind, statusCode, requestId));

    private static string? GetRequestId(IReadOnlyDictionary<string, string>? headers) =>
        SelectRequestId(
            GetHeaderValue(headers, "request-id"),
            GetHeaderValue(headers, "client-request-id"));

    private static string? GetHeaderValue(
        IReadOnlyDictionary<string, string>? headers,
        string headerName)
    {
        if (headers is null)
        {
            return null;
        }

        return headers
            .Where(pair => string.Equals(pair.Key, headerName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => NormalizeRequestId(pair.Value))
            .FirstOrDefault(value => value is not null);
    }

    private static string? NormalizeRequestId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl))
        {
            return null;
        }

        return value.Trim();
    }
}
