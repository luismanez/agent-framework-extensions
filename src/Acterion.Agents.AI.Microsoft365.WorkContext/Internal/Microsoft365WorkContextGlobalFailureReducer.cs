using System.Net;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal static class Microsoft365WorkContextGlobalFailureReducer
{
    internal static WorkContextSnapshot Reduce(
        DateTimeOffset capturedAtUtc,
        Microsoft365WorkContextOptionsSnapshot options,
        WorkContextFailureKind failureKind,
        HttpStatusCode? statusCode = null,
        string? requestId = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        WorkContextFacetFailure failure = new(failureKind, statusCode, requestId);
        return new WorkContextSnapshot(
            capturedAtUtc,
            Result<WorkContextUserProfile>(options.EnableUserProfile, failure),
            Result<WorkContextManager>(options.EnableManager, failure),
            Result<WorkContextWorkSettings>(options.EnableWorkSettings, failure),
            Result<IReadOnlyList<WorkContextCalendarEvent>>(options.EnableCalendar, failure));
    }

    private static WorkContextFacetResult<T> Result<T>(
        bool enabled,
        WorkContextFacetFailure failure)
        where T : class =>
        enabled
            ? new WorkContextFacetResult<T>(WorkContextFacetStatus.Failed, value: null, failure)
            : new WorkContextFacetResult<T>(WorkContextFacetStatus.Disabled, value: null, failure: null);
}
