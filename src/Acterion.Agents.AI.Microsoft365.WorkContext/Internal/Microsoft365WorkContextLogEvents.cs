using Microsoft.Extensions.Logging;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal static class Microsoft365WorkContextLogEvents
{
    internal static readonly EventId Started = new(2000, nameof(Started));
    internal static readonly EventId Completed = new(2001, nameof(Completed));
    internal static readonly EventId Failed = new(2002, nameof(Failed));

    internal static void LogStarted(ILogger logger, int operationCount, bool isBatch) =>
        logger.LogInformation(
            Started,
            "Microsoft 365 work-context retrieval started. Operation count: {OperationCount}. Batch: {IsBatch}.",
            operationCount,
            isBatch);

    internal static void LogCompleted(
        ILogger logger,
        double elapsedMilliseconds,
        WorkContextSnapshot snapshot)
    {
        logger.LogInformation(
            Completed,
            "Microsoft 365 work-context retrieval completed. Elapsed milliseconds: {ElapsedMilliseconds}. " +
            "Profile: {UserProfileStatus}. Manager: {ManagerStatus}. Work settings: {WorkSettingsStatus}. " +
            "Calendar: {CalendarStatus}. Event count: {EventCount}. Attendees truncated: {AttendeesTruncated}.",
            Math.Max(0, (long)elapsedMilliseconds),
            snapshot.UserProfile.Status,
            snapshot.Manager.Status,
            snapshot.WorkSettings.Status,
            snapshot.Calendar.Status,
            snapshot.Calendar.Value?.Count ?? 0,
            snapshot.Calendar.Value?.Any(item => item.AreAttendeesTruncated) ?? false);
    }

    internal static void LogSnapshotFailures(
        ILogger logger,
        double elapsedMilliseconds,
        WorkContextSnapshot snapshot)
    {
        if (snapshot.UserProfile.Failure is { } profile)
        {
            LogFacetFailure(logger, elapsedMilliseconds, WorkContextFacet.UserProfile, profile);
        }

        if (snapshot.Manager.Failure is { } manager)
        {
            LogFacetFailure(logger, elapsedMilliseconds, WorkContextFacet.Manager, manager);
        }

        if (snapshot.WorkSettings.Failure is { } workSettings)
        {
            LogFacetFailure(logger, elapsedMilliseconds, WorkContextFacet.WorkSettings, workSettings);
        }

        if (snapshot.Calendar.Failure is { } calendar)
        {
            LogFacetFailure(logger, elapsedMilliseconds, WorkContextFacet.Calendar, calendar);
        }
    }

    internal static void LogFailed(
        ILogger logger,
        double elapsedMilliseconds,
        Microsoft365WorkContextException exception) =>
        LogFailure(logger, elapsedMilliseconds, exception.Facet, exception.Kind,
            (int?)exception.StatusCode, exception.RequestId);

    private static void LogFacetFailure(
        ILogger logger,
        double elapsedMilliseconds,
        WorkContextFacet facet,
        WorkContextFacetFailure failure) =>
        LogFailure(logger, elapsedMilliseconds, facet, failure.Kind,
            (int?)failure.StatusCode, failure.RequestId);

    private static void LogFailure(
        ILogger logger,
        double elapsedMilliseconds,
        WorkContextFacet? facet,
        WorkContextFailureKind failureKind,
        int? statusCode,
        string? requestId) =>
        logger.LogWarning(
            Failed,
            "Microsoft 365 work-context retrieval failed. Elapsed milliseconds: {ElapsedMilliseconds}. " +
            "Facet: {Facet}. Failure kind: {FailureKind}. Status code: {StatusCode}. Request id: {RequestId}.",
            Math.Max(0, (long)elapsedMilliseconds),
            facet,
            failureKind,
            statusCode,
            requestId);
}
