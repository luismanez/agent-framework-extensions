using System.Globalization;
using System.Net;
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
                ? ReduceCalendar(GetOutcome(outcomes, WorkContextFacet.Calendar), options.MaximumCalendarEvents)
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
        MicrosoftGraphOperationOutcome timeZone = GetOutcome(outcomes, "work-time-zone");
        MicrosoftGraphOperationOutcome language = GetOutcome(outcomes, "work-language");
        MicrosoftGraphOperationOutcome workingHours = GetOutcome(outcomes, "work-hours");

        WorkSettingsChildResult<string> timeZoneResult = ReduceWorkSettingsChild(timeZone, MapTimeZone);
        WorkSettingsChildResult<WorkContextLocale> languageResult = ReduceWorkSettingsChild(language, MapLanguage);
        WorkSettingsChildResult<WorkContextWorkingHours> workingHoursResult = ReduceWorkSettingsChild(workingHours, MapWorkingHours);

        WorkContextFacetFailure? failure = timeZoneResult.Failure ??
            languageResult.Failure ??
            workingHoursResult.Failure;
        bool hasSuccess = timeZoneResult.IsSuccessful ||
            languageResult.IsSuccessful ||
            workingHoursResult.IsSuccessful;

        WorkContextWorkSettings? value = hasSuccess
            ? new WorkContextWorkSettings(
                timeZoneResult.Value,
                languageResult.Value,
                workingHoursResult.Value)
            : null;

        if (failure is not null)
        {
            return new WorkContextFacetResult<WorkContextWorkSettings>(
                WorkContextFacetStatus.Failed,
                value,
                failure);
        }

        if (!hasSuccess)
        {
            return Unavailable<WorkContextWorkSettings>();
        }

        return Available(value!);
    }

    private static WorkSettingsChildResult<T> ReduceWorkSettingsChild<T>(
        MicrosoftGraphOperationOutcome outcome,
        Func<JsonElement, T> map)
        where T : class
    {
        if (outcome.Status == MicrosoftGraphOperationOutcomeStatus.Failed)
        {
            return new WorkSettingsChildResult<T>(false, null, outcome.Failure!);
        }

        if (outcome.Status == MicrosoftGraphOperationOutcomeStatus.Unavailable)
        {
            return new WorkSettingsChildResult<T>(false, null, null);
        }

        try
        {
            return new WorkSettingsChildResult<T>(true, map(outcome.Payload!.Value), null);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            return new WorkSettingsChildResult<T>(
                false,
                null,
                new WorkContextFacetFailure(
                    WorkContextFailureKind.InvalidResponse,
                    HttpStatusCode.OK,
                    requestId: null));
        }
    }

    private static WorkContextFacetResult<IReadOnlyList<WorkContextCalendarEvent>> ReduceCalendar(
        MicrosoftGraphOperationOutcome outcome,
        int maximumEvents)
    {
        if (outcome.Status == MicrosoftGraphOperationOutcomeStatus.Failed)
        {
            return Failed<IReadOnlyList<WorkContextCalendarEvent>>(outcome.Failure!);
        }

        try
        {
            MicrosoftGraphCalendarResponse response = outcome.Payload!.Value
                .Deserialize<MicrosoftGraphCalendarResponse>()
                ?? throw new InvalidDataException("The Calendar response body is invalid.");
            MicrosoftGraphCalendarMappingResult result = MicrosoftGraphCalendarMapper.Map(response, maximumEvents);
            return result.HasInvalidEvents
                ? new WorkContextFacetResult<IReadOnlyList<WorkContextCalendarEvent>>(
                    WorkContextFacetStatus.Failed,
                    result.Events,
                    new WorkContextFacetFailure(WorkContextFailureKind.InvalidResponse, HttpStatusCode.OK, requestId: null))
                : Available(result.Events);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            return Failed<IReadOnlyList<WorkContextCalendarEvent>>(
                new WorkContextFacetFailure(WorkContextFailureKind.InvalidResponse, HttpStatusCode.OK, requestId: null));
        }
    }

    private static MicrosoftGraphOperationOutcome GetOutcome(
        IReadOnlyList<MicrosoftGraphOperationOutcome> outcomes,
        WorkContextFacet facet) =>
        outcomes.Single(outcome => outcome.Operation.Facet == facet);

    private static MicrosoftGraphOperationOutcome GetOutcome(
        IReadOnlyList<MicrosoftGraphOperationOutcome> outcomes,
        string operationId) =>
        outcomes.Single(outcome => string.Equals(
            outcome.Operation.Id,
            operationId,
            StringComparison.Ordinal));

    private static string MapTimeZone(JsonElement payload) =>
        payload.GetString() ?? throw new InvalidDataException("The Work Settings time-zone response body is invalid.");

    private static WorkContextLocale MapLanguage(JsonElement payload)
    {
        MicrosoftGraphLocaleInfoResponse response =
            payload.Deserialize<MicrosoftGraphLocaleInfoResponse>()
            ?? throw new InvalidDataException("The Work Settings language response body is invalid.");

        return new WorkContextLocale(response.Locale, response.DisplayName);
    }

    private static WorkContextWorkingHours MapWorkingHours(JsonElement payload)
    {
        MicrosoftGraphWorkingHoursResponse response =
            payload.Deserialize<MicrosoftGraphWorkingHoursResponse>()
            ?? throw new InvalidDataException("The Work Settings working-hours response body is invalid.");

        if (response.DaysOfWeek is null ||
            !TryParseTime(response.StartTime, out TimeOnly startTime) ||
            !TryParseTime(response.EndTime, out TimeOnly endTime))
        {
            throw new InvalidDataException("The Work Settings working-hours response body is invalid.");
        }

        DayOfWeek[] daysOfWeek = response.DaysOfWeek
            .Select(ParseDayOfWeek)
            .ToArray();

        return new WorkContextWorkingHours(
            daysOfWeek,
            startTime,
            endTime,
            response.TimeZone?.Name);
    }

    private static DayOfWeek ParseDayOfWeek(string value)
    {
        if (!Enum.TryParse(value, ignoreCase: true, out DayOfWeek dayOfWeek) ||
            !Enum.IsDefined(dayOfWeek))
        {
            throw new InvalidDataException("The Work Settings working-days response body is invalid.");
        }

        return dayOfWeek;
    }

    private static bool TryParseTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(
            value,
            ["HH:mm:ss", "HH:mm:ss.FFFFFFF"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out time);

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

    private readonly record struct WorkSettingsChildResult<T>(
        bool IsSuccessful,
        T? Value,
        WorkContextFacetFailure? Failure)
        where T : class;
}
