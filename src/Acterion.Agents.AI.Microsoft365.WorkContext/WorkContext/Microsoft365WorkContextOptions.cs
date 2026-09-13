namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Configures the facets and behavior of Microsoft 365 work-context retrieval.</summary>
public sealed class Microsoft365WorkContextOptions
{
    /// <summary>Gets or sets whether the signed-in user's profile is retrieved.</summary>
    public bool EnableUserProfile { get; set; } = true;

    /// <summary>Gets or sets whether the signed-in user's immediate manager is retrieved.</summary>
    public bool EnableManager { get; set; }

    /// <summary>Gets or sets whether mailbox work settings are retrieved.</summary>
    public bool EnableWorkSettings { get; set; }

    /// <summary>Gets or sets whether the default calendar view is retrieved.</summary>
    public bool EnableCalendar { get; set; }

    /// <summary>Gets or sets how retrieval failures are handled.</summary>
    public WorkContextErrorBehavior ErrorBehavior { get; set; } = WorkContextErrorBehavior.BestEffort;

    /// <summary>Gets or sets the future interval included in the calendar view.</summary>
    public TimeSpan CalendarLookAhead { get; set; } = TimeSpan.FromHours(24);

    /// <summary>Gets or sets the maximum Calendar page size requested from Microsoft Graph.</summary>
    public int MaximumCalendarEvents { get; set; } = 10;
}