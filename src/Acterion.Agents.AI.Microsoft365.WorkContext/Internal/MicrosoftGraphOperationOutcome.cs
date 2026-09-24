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
        WorkContextFacetFailure? failure)
    {
        Operation = operation;
        Status = status;
        Payload = payload;
        Failure = failure;
    }

    internal MicrosoftGraphOperation Operation { get; }

    internal MicrosoftGraphOperationOutcomeStatus Status { get; }

    internal JsonElement? Payload { get; }

    internal WorkContextFacetFailure? Failure { get; }

    internal static MicrosoftGraphOperationOutcome Succeeded(
        MicrosoftGraphOperation operation,
        JsonElement payload) =>
        new(operation, MicrosoftGraphOperationOutcomeStatus.Succeeded, payload.Clone(), failure: null);

    internal static MicrosoftGraphOperationOutcome Unavailable(MicrosoftGraphOperation operation) =>
        new(operation, MicrosoftGraphOperationOutcomeStatus.Unavailable, payload: null, failure: null);

    internal static MicrosoftGraphOperationOutcome Failed(
        MicrosoftGraphOperation operation,
        WorkContextFacetFailure failure) =>
        new(operation, MicrosoftGraphOperationOutcomeStatus.Failed, payload: null, failure);
}
