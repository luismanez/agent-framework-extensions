using Microsoft.Extensions.Logging;

namespace Acterion.Agents.AI.Microsoft365.Retrieval;

internal static class RetrievalLogEvents
{
    internal static readonly EventId RetrievalStarted = new(1000, nameof(RetrievalStarted));
    internal static readonly EventId RetrievalCompleted = new(1001, nameof(RetrievalCompleted));
    internal static readonly EventId RetrievalFailed = new(1002, nameof(RetrievalFailed));
    internal static readonly EventId RetrievalThrottled = new(1003, nameof(RetrievalThrottled));

    internal static void LogStarted(
        ILogger logger,
        int maximumNumberOfResults,
        bool hasFilter)
    {
        logger.LogInformation(
            RetrievalStarted,
            "Microsoft 365 retrieval started. Maximum results: {MaximumNumberOfResults}. Filter configured: {HasFilter}.",
            maximumNumberOfResults,
            hasFilter);
    }

    internal static void LogCompleted(
        ILogger logger,
        long elapsedMilliseconds,
        int resultCount)
    {
        logger.LogInformation(
            RetrievalCompleted,
            "Microsoft 365 retrieval completed. Elapsed milliseconds: {ElapsedMilliseconds}. Result count: {ResultCount}.",
            elapsedMilliseconds,
            resultCount);
    }

    internal static void LogFailed(
        ILogger logger,
        long elapsedMilliseconds,
        int? statusCode)
    {
        logger.LogWarning(
            RetrievalFailed,
            "Microsoft 365 retrieval failed. Elapsed milliseconds: {ElapsedMilliseconds}. Status code: {StatusCode}.",
            elapsedMilliseconds,
            statusCode);
    }

    internal static void LogThrottled(ILogger logger, long elapsedMilliseconds)
    {
        logger.LogWarning(
            RetrievalThrottled,
            "Microsoft 365 retrieval was throttled. Elapsed milliseconds: {ElapsedMilliseconds}.",
            elapsedMilliseconds);
    }
}
