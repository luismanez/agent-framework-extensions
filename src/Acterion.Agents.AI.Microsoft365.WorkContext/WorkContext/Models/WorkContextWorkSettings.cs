namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Contains the signed-in user's allowlisted mailbox work settings.</summary>
public sealed class WorkContextWorkSettings
{
    internal WorkContextWorkSettings(
        string? timeZone,
        WorkContextLocale? language,
        WorkContextWorkingHours? workingHours)
    {
        TimeZone = timeZone;
        Language = language;
        WorkingHours = workingHours;
    }

    /// <summary>Gets the mailbox time-zone identifier.</summary>
    public string? TimeZone { get; }

    /// <summary>Gets the mailbox language settings.</summary>
    public WorkContextLocale? Language { get; }

    /// <summary>Gets the mailbox working-hours settings.</summary>
    public WorkContextWorkingHours? WorkingHours { get; }
}