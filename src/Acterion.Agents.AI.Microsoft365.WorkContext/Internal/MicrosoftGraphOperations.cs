using System.Globalization;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal static class MicrosoftGraphOperations
{
    private const string CalendarSelect =
        "subject,start,end,originalStartTimeZone,originalEndTimeZone,location,organizer,attendees," +
        "isAllDay,isCancelled,sensitivity,showAs,responseStatus";

    internal static readonly Uri Profile = new(
        "v1.0/me?%24select=displayName%2CgivenName%2Csurname%2CjobTitle%2Cdepartment%2CofficeLocation%2CpreferredLanguage",
        UriKind.Relative);

    internal static readonly Uri Manager = new(
        "v1.0/me/manager?%24select=displayName%2CjobTitle%2Cdepartment%2CofficeLocation",
        UriKind.Relative);

    internal static Uri CreateCalendarUri(
        DateTimeOffset capturedAtUtc,
        TimeSpan lookAhead,
        int maximumCalendarEvents)
    {
        DateTimeOffset startUtc = capturedAtUtc.ToUniversalTime();
        DateTimeOffset endUtc = startUtc.Add(lookAhead);
        string start = Uri.EscapeDataString(startUtc.ToString("O", CultureInfo.InvariantCulture));
        string end = Uri.EscapeDataString(endUtc.ToString("O", CultureInfo.InvariantCulture));
        string maximum = maximumCalendarEvents.ToString(CultureInfo.InvariantCulture);
        string select = Uri.EscapeDataString(CalendarSelect);

        return new Uri(
            $"v1.0/me/calendar/calendarView?startDateTime={start}" +
            $"&endDateTime={end}&%24top={maximum}&%24select={select}",
            UriKind.Relative);
    }
}