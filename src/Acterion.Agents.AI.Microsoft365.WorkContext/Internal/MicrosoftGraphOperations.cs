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

    private static readonly MicrosoftGraphOperation ProfileOperation = new(
        "profile",
        WorkContextFacet.UserProfile,
        Profile,
        "/me?%24select=displayName%2CgivenName%2Csurname%2CjobTitle%2Cdepartment%2CofficeLocation%2CpreferredLanguage");

    private static readonly MicrosoftGraphOperation ManagerOperation = new(
        "manager",
        WorkContextFacet.Manager,
        Manager,
        "/me/manager?%24select=displayName%2CjobTitle%2Cdepartment%2CofficeLocation");

    private static readonly MicrosoftGraphOperation WorkTimeZoneOperation = new(
        "work-time-zone",
        WorkContextFacet.WorkSettings,
        new Uri("v1.0/me/mailboxSettings/timeZone", UriKind.Relative),
        "/me/mailboxSettings/timeZone");

    private static readonly MicrosoftGraphOperation WorkLanguageOperation = new(
        "work-language",
        WorkContextFacet.WorkSettings,
        new Uri("v1.0/me/mailboxSettings/language", UriKind.Relative),
        "/me/mailboxSettings/language");

    private static readonly MicrosoftGraphOperation WorkHoursOperation = new(
        "work-hours",
        WorkContextFacet.WorkSettings,
        new Uri("v1.0/me/mailboxSettings/workingHours", UriKind.Relative),
        "/me/mailboxSettings/workingHours");

    internal static IReadOnlyList<MicrosoftGraphOperation> Select(
        Microsoft365WorkContextOptionsSnapshot options,
        DateTimeOffset capturedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<MicrosoftGraphOperation> operations = [];

        if (options.EnableUserProfile)
        {
            operations.Add(ProfileOperation);
        }

        if (options.EnableManager)
        {
            operations.Add(ManagerOperation);
        }

        if (options.EnableWorkSettings)
        {
            operations.Add(WorkTimeZoneOperation);
            operations.Add(WorkLanguageOperation);
            operations.Add(WorkHoursOperation);
        }

        if (options.EnableCalendar)
        {
            Uri directUri = CreateCalendarUri(
                capturedAtUtc,
                options.CalendarLookAhead,
                options.MaximumCalendarEvents);
            operations.Add(new MicrosoftGraphOperation(
                "calendar",
                WorkContextFacet.Calendar,
                directUri,
                directUri.OriginalString["v1.0".Length..]));
        }

        return operations.AsReadOnly();
    }

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