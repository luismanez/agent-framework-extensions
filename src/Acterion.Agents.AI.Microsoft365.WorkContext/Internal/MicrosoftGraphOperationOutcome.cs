using System.Net;
using System.Text.Json;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal enum MicrosoftGraphOperationOutcomeStatus
{
    Succeeded,
    Unavailable,
    Failed,
}

internal sealed class MicrosoftGraphOperationOutcome
{
    private MicrosoftGraphOperationOutcome(
        MicrosoftGraphOperation operation,
        MicrosoftGraphOperationOutcomeStatus status,
        JsonElement? payload,
        WorkContextFacetFailure? failure,
        HttpStatusCode? responseStatusCode,
        string? requestId)
    {
        Operation = operation;
        Status = status;
        Payload = payload;
        Failure = failure;
        ResponseStatusCode = responseStatusCode;
        RequestId = requestId;
    }

    internal MicrosoftGraphOperation Operation { get; }

    internal MicrosoftGraphOperationOutcomeStatus Status { get; }

    internal JsonElement? Payload { get; }

    internal WorkContextFacetFailure? Failure { get; }

    internal HttpStatusCode? ResponseStatusCode { get; }

    internal string? RequestId { get; }

    internal static MicrosoftGraphOperationOutcome Succeeded(
        MicrosoftGraphOperation operation,
        JsonElement payload,
        HttpStatusCode responseStatusCode,
        string? requestId) =>
        new(operation, MicrosoftGraphOperationOutcomeStatus.Succeeded, payload.Clone(),
            failure: null, responseStatusCode, requestId);

    internal static MicrosoftGraphOperationOutcome Unavailable(MicrosoftGraphOperation operation) =>
        new(operation, MicrosoftGraphOperationOutcomeStatus.Unavailable, payload: null, failure: null,
            responseStatusCode: null, requestId: null);

    internal static MicrosoftGraphOperationOutcome Failed(
        MicrosoftGraphOperation operation,
        WorkContextFacetFailure failure) =>
        new(operation, MicrosoftGraphOperationOutcomeStatus.Failed, payload: null, failure,
            failure.StatusCode, failure.RequestId);
}
