using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal sealed class MicrosoftGraphCalendarResponse
{
    [JsonPropertyName("value")]
    public IReadOnlyList<JsonElement>? Value { get; init; }
}

internal sealed class MicrosoftGraphCalendarEventResponse
{
    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    [JsonPropertyName("start")]
    public MicrosoftGraphCalendarDateTimeResponse? Start { get; init; }

    [JsonPropertyName("end")]
    public MicrosoftGraphCalendarDateTimeResponse? End { get; init; }

    [JsonPropertyName("originalStartTimeZone")]
    public string? OriginalStartTimeZone { get; init; }

    [JsonPropertyName("originalEndTimeZone")]
    public string? OriginalEndTimeZone { get; init; }

    [JsonPropertyName("location")]
    public MicrosoftGraphCalendarLocationResponse? Location { get; init; }

    [JsonPropertyName("organizer")]
    public MicrosoftGraphCalendarAttendeeResponse? Organizer { get; init; }

    [JsonPropertyName("attendees")]
    public IReadOnlyList<MicrosoftGraphCalendarAttendeeResponse?>? Attendees { get; init; }

    [JsonPropertyName("isAllDay")]
    public bool IsAllDay { get; init; }

    [JsonPropertyName("isCancelled")]
    public bool IsCancelled { get; init; }

    [JsonPropertyName("sensitivity")]
    public string? Sensitivity { get; init; }

    [JsonPropertyName("showAs")]
    public string? ShowAs { get; init; }

    [JsonPropertyName("responseStatus")]
    public MicrosoftGraphCalendarResponseStatusResponse? ResponseStatus { get; init; }
}

internal sealed class MicrosoftGraphCalendarDateTimeResponse
{
    [JsonPropertyName("dateTime")]
    public string? DateTime { get; init; }
}

internal sealed class MicrosoftGraphCalendarLocationResponse
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}

internal sealed class MicrosoftGraphCalendarAttendeeResponse
{
    [JsonPropertyName("emailAddress")]
    public MicrosoftGraphCalendarNameResponse? EmailAddress { get; init; }
}

internal sealed class MicrosoftGraphCalendarNameResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

internal sealed class MicrosoftGraphCalendarResponseStatusResponse
{
    [JsonPropertyName("response")]
    public string? Response { get; init; }
}

internal static class MicrosoftGraphCalendarMapper
{
    internal static MicrosoftGraphCalendarMappingResult Map(
        MicrosoftGraphCalendarResponse response,
        int maximumEvents)
    {
        if (response.Value is null)
        {
            throw new InvalidDataException("The Calendar response body is invalid.");
        }

        List<WorkContextCalendarEvent> validEvents = [];
        bool hasInvalidEvents = false;
        foreach (JsonElement element in response.Value)
        {
            try
            {
                MicrosoftGraphCalendarEventResponse item = element.Deserialize<MicrosoftGraphCalendarEventResponse>()
                    ?? throw new InvalidDataException("The Calendar event body is invalid.");
                if (item.IsCancelled ||
                    string.Equals(item.ResponseStatus?.Response, "declined", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                validEvents.Add(MapEvent(item));
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException)
            {
                hasInvalidEvents = true;
            }
        }

        WorkContextCalendarEvent[] events = validEvents
            .OrderBy(item => item.StartUtc)
            .ThenBy(item => item.EndUtc)
            .Take(maximumEvents)
            .ToArray();

        return new MicrosoftGraphCalendarMappingResult(Array.AsReadOnly(events), hasInvalidEvents);
    }

    private static WorkContextCalendarEvent MapEvent(MicrosoftGraphCalendarEventResponse response)
    {
        if (!IsKnownSensitivity(response.Sensitivity) ||
            !IsKnownAvailability(response.ShowAs) ||
            !IsKnownResponseStatus(response.ResponseStatus?.Response))
        {
            throw new InvalidDataException("The Calendar event metadata is invalid.");
        }

        if (!TryParseUtc(response.Start?.DateTime, out DateTimeOffset startUtc) ||
            !TryParseUtc(response.End?.DateTime, out DateTimeOffset endUtc) ||
            endUtc < startUtc)
        {
            throw new InvalidDataException("The Calendar event time is invalid.");
        }

        bool isPrivate = string.Equals(response.Sensitivity, "private", StringComparison.OrdinalIgnoreCase);
        IReadOnlyList<MicrosoftGraphCalendarAttendeeResponse?> attendees = response.Attendees ?? [];
        if (attendees.Any(item => item is null))
        {
            throw new InvalidDataException("The Calendar attendees response is invalid.");
        }

        string[] attendeeNames = isPrivate
            ? []
            : attendees
                .Select(item => item!.EmailAddress?.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Take(10)
                .Select(name => name!)
                .ToArray();

        return new WorkContextCalendarEvent(
            startUtc,
            endUtc,
            response.OriginalStartTimeZone,
            response.OriginalEndTimeZone,
            isPrivate ? null : response.Subject,
            isPrivate ? null : response.Location?.DisplayName,
            isPrivate ? null : response.Organizer?.EmailAddress?.Name,
            attendeeNames,
            areAttendeesTruncated: !isPrivate && attendees.Count > 10,
            response.IsAllDay,
            isPrivate,
            response.ShowAs);
    }

    private static bool TryParseUtc(string? value, out DateTimeOffset utc)
    {
        utc = default;
        return value?.Contains('T') == true && DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out utc);
    }

    private static bool IsKnownSensitivity(string? value) =>
        value is not null &&
        (value.Equals("normal", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("personal", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("private", StringComparison.OrdinalIgnoreCase) ||
         value.Equals("confidential", StringComparison.OrdinalIgnoreCase));

    private static bool IsKnownAvailability(string? value) =>
        value is null ||
        value.Equals("free", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("tentative", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("busy", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("oof", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("workingElsewhere", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("unknown", StringComparison.OrdinalIgnoreCase);

    private static bool IsKnownResponseStatus(string? value) =>
        value is null ||
        value.Equals("none", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("organizer", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("tentativelyAccepted", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("accepted", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("declined", StringComparison.OrdinalIgnoreCase) ||
        value.Equals("notResponded", StringComparison.OrdinalIgnoreCase);
}

internal sealed record MicrosoftGraphCalendarMappingResult(
    IReadOnlyList<WorkContextCalendarEvent> Events,
    bool HasInvalidEvents);
