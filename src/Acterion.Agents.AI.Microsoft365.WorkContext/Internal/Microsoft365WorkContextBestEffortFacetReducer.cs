using System.Text.Json;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal static class Microsoft365WorkContextBestEffortFacetReducer
{
    internal static WorkContextSnapshot Reduce(
        DateTimeOffset capturedAtUtc,
        Microsoft365WorkContextOptionsSnapshot options,
        IReadOnlyList<MicrosoftGraphOperationOutcome> outcomes)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(outcomes);

        return new WorkContextSnapshot(
            capturedAtUtc,
            options.EnableUserProfile
                ? ReduceProfile(GetOutcome(outcomes, WorkContextFacet.UserProfile))
                : Disabled<WorkContextUserProfile>(),
            options.EnableManager
                ? ReduceManager(GetOutcome(outcomes, WorkContextFacet.Manager))
                : Disabled<WorkContextManager>(),
            options.EnableWorkSettings
                ? ReduceWorkSettings(outcomes)
                : Disabled<WorkContextWorkSettings>(),
            options.EnableCalendar
                ? ReduceCalendar(GetOutcome(outcomes, WorkContextFacet.Calendar))
                : Disabled<IReadOnlyList<WorkContextCalendarEvent>>());
    }

    private static WorkContextFacetResult<WorkContextUserProfile> ReduceProfile(
        MicrosoftGraphOperationOutcome outcome) =>
        outcome.Status switch
        {
            MicrosoftGraphOperationOutcomeStatus.Succeeded => Available(MapProfile(outcome.Payload!.Value)),
            MicrosoftGraphOperationOutcomeStatus.Unavailable => Unavailable<WorkContextUserProfile>(),
            MicrosoftGraphOperationOutcomeStatus.Failed => Failed<WorkContextUserProfile>(outcome.Failure!),
            _ => throw new InvalidOperationException("The Profile operation outcome is invalid."),
        };

    private static WorkContextFacetResult<WorkContextManager> ReduceManager(
        MicrosoftGraphOperationOutcome outcome) =>
        outcome.Status switch
        {
            MicrosoftGraphOperationOutcomeStatus.Succeeded => Available(MapManager(outcome.Payload!.Value)),
            MicrosoftGraphOperationOutcomeStatus.Unavailable => Unavailable<WorkContextManager>(),
            MicrosoftGraphOperationOutcomeStatus.Failed => Failed<WorkContextManager>(outcome.Failure!),
            _ => throw new InvalidOperationException("The Manager operation outcome is invalid."),
        };

    private static WorkContextFacetResult<WorkContextWorkSettings> ReduceWorkSettings(
        IReadOnlyList<MicrosoftGraphOperationOutcome> outcomes)
    {
        MicrosoftGraphOperationOutcome? failure = outcomes.FirstOrDefault(
            outcome => outcome.Operation.Facet == WorkContextFacet.WorkSettings &&
                outcome.Status == MicrosoftGraphOperationOutcomeStatus.Failed);

        return failure is not null
            ? Failed<WorkContextWorkSettings>(failure.Failure!)
            : throw new InvalidOperationException("Work Settings mapping is not available.");
    }

    private static WorkContextFacetResult<IReadOnlyList<WorkContextCalendarEvent>> ReduceCalendar(
        MicrosoftGraphOperationOutcome outcome) =>
        outcome.Status == MicrosoftGraphOperationOutcomeStatus.Failed
            ? Failed<IReadOnlyList<WorkContextCalendarEvent>>(outcome.Failure!)
            : throw new InvalidOperationException("Calendar mapping is not available.");

    private static MicrosoftGraphOperationOutcome GetOutcome(
        IReadOnlyList<MicrosoftGraphOperationOutcome> outcomes,
        WorkContextFacet facet) =>
        outcomes.Single(outcome => outcome.Operation.Facet == facet);

    private static WorkContextUserProfile MapProfile(JsonElement payload)
    {
        MicrosoftGraphUserProfileResponse response =
            payload.Deserialize<MicrosoftGraphUserProfileResponse>()
            ?? throw new InvalidDataException("The Profile response body is invalid.");

        return new WorkContextUserProfile(
            response.DisplayName,
            response.GivenName,
            response.Surname,
            response.JobTitle,
            response.Department,
            response.OfficeLocation,
            response.PreferredLanguage);
    }

    private static WorkContextManager MapManager(JsonElement payload)
    {
        MicrosoftGraphManagerResponse response =
            payload.Deserialize<MicrosoftGraphManagerResponse>()
            ?? throw new InvalidDataException("The Manager response body is invalid.");

        return new WorkContextManager(
            response.DisplayName,
            response.JobTitle,
            response.Department,
            response.OfficeLocation);
    }

    private static WorkContextFacetResult<T> Available<T>(T value) where T : class =>
        new(WorkContextFacetStatus.Available, value, failure: null);

    private static WorkContextFacetResult<T> Unavailable<T>() where T : class =>
        new(WorkContextFacetStatus.Unavailable, value: null, failure: null);

    private static WorkContextFacetResult<T> Failed<T>(WorkContextFacetFailure failure) where T : class =>
        new(WorkContextFacetStatus.Failed, value: null, failure);

    private static WorkContextFacetResult<T> Disabled<T>() where T : class =>
        new(WorkContextFacetStatus.Disabled, value: null, failure: null);
}
