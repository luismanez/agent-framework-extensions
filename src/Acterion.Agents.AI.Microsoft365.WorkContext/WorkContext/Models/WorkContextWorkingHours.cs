using System.Collections.ObjectModel;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Contains the signed-in user's working days, times, and time-zone name.</summary>
public sealed class WorkContextWorkingHours
{
    internal WorkContextWorkingHours(
        IReadOnlyList<DayOfWeek> daysOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        string? timeZone)
    {
        ArgumentNullException.ThrowIfNull(daysOfWeek);

        DaysOfWeek = new ReadOnlyCollection<DayOfWeek>(daysOfWeek.ToArray());
        StartTime = startTime;
        EndTime = endTime;
        TimeZone = timeZone;
    }

    /// <summary>Gets an immutable snapshot of the working days.</summary>
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; }

    /// <summary>Gets the local start time.</summary>
    public TimeOnly StartTime { get; }

    /// <summary>Gets the local end time.</summary>
    public TimeOnly EndTime { get; }

    /// <summary>Gets the opaque working-hours time-zone name.</summary>
    public string? TimeZone { get; }
}