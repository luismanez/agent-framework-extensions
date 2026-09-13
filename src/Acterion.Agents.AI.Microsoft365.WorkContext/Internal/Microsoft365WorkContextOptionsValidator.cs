using Microsoft.Extensions.Options;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal static class Microsoft365WorkContextOptionsValidator
{
    internal static Microsoft365WorkContextOptionsSnapshot ValidateAndSnapshot(
        Microsoft365WorkContextOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        List<string> failures = [];

        if (options.CalendarLookAhead <= TimeSpan.Zero || options.CalendarLookAhead > TimeSpan.FromDays(7))
        {
            failures.Add("CalendarLookAhead must be greater than zero and no greater than seven days.");
        }

        if (options.MaximumCalendarEvents is < 1 or > 25)
        {
            failures.Add("MaximumCalendarEvents must be between 1 and 25.");
        }

        if (!Enum.IsDefined(options.ErrorBehavior))
        {
            failures.Add("ErrorBehavior must be a defined value.");
        }

        if (failures.Count > 0)
        {
            throw new OptionsValidationException(
                Options.DefaultName,
                typeof(Microsoft365WorkContextOptions),
                failures);
        }

        return new Microsoft365WorkContextOptionsSnapshot(
            options.EnableUserProfile,
            options.EnableManager,
            options.EnableWorkSettings,
            options.EnableCalendar,
            options.ErrorBehavior,
            options.CalendarLookAhead,
            options.MaximumCalendarEvents);
    }
}

internal sealed record Microsoft365WorkContextOptionsSnapshot(
    bool EnableUserProfile,
    bool EnableManager,
    bool EnableWorkSettings,
    bool EnableCalendar,
    WorkContextErrorBehavior ErrorBehavior,
    TimeSpan CalendarLookAhead,
    int MaximumCalendarEvents)
{
    internal bool HasEnabledFacet =>
        EnableUserProfile || EnableManager || EnableWorkSettings || EnableCalendar;
}