using System.Collections.ObjectModel;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Contains allowlisted metadata for one event in the signed-in user's calendar view.</summary>
public sealed class WorkContextCalendarEvent
{
    internal WorkContextCalendarEvent(
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        string? originalStartTimeZone,
        string? originalEndTimeZone,
        string? subject,
        string? location,
        string? organizerName,
        IReadOnlyList<string> attendeeNames,
        bool areAttendeesTruncated,
        bool isAllDay,
        bool isPrivate,
        string? availability)
    {
        ArgumentNullException.ThrowIfNull(attendeeNames);

        StartUtc = startUtc;
        EndUtc = endUtc;
        OriginalStartTimeZone = originalStartTimeZone;
        OriginalEndTimeZone = originalEndTimeZone;
        Subject = subject;
        Location = location;
        OrganizerName = organizerName;
        AttendeeNames = new ReadOnlyCollection<string>(attendeeNames.ToArray());
        AreAttendeesTruncated = areAttendeesTruncated;
        IsAllDay = isAllDay;
        IsPrivate = isPrivate;
        Availability = availability;
    }

    /// <summary>Gets the event start time in UTC.</summary>
    public DateTimeOffset StartUtc { get; }

    /// <summary>Gets the event end time in UTC.</summary>
    public DateTimeOffset EndUtc { get; }

    /// <summary>Gets the original start time-zone name.</summary>
    public string? OriginalStartTimeZone { get; }

    /// <summary>Gets the original end time-zone name.</summary>
    public string? OriginalEndTimeZone { get; }

    /// <summary>Gets the event subject when privacy policy permits it.</summary>
    public string? Subject { get; }

    /// <summary>Gets the location display name when privacy policy permits it.</summary>
    public string? Location { get; }

    /// <summary>Gets the organizer display name when privacy policy permits it.</summary>
    public string? OrganizerName { get; }

    /// <summary>Gets an immutable snapshot of attendee display names allowed by privacy policy.</summary>
    public IReadOnlyList<string> AttendeeNames { get; }

    /// <summary>Gets whether additional attendee names were omitted.</summary>
    public bool AreAttendeesTruncated { get; }

    /// <summary>Gets whether the event spans an entire day.</summary>
    public bool IsAllDay { get; }

    /// <summary>Gets whether the event is private.</summary>
    public bool IsPrivate { get; }

    /// <summary>Gets the event availability value.</summary>
    public string? Availability { get; }
}