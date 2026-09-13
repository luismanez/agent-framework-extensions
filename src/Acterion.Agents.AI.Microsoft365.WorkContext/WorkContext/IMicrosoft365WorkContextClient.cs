namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Retrieves a fresh snapshot of the signed-in user's Microsoft 365 work context.</summary>
public interface IMicrosoft365WorkContextClient
{
    /// <summary>Retrieves a fresh work-context snapshot.</summary>
    /// <param name="cancellationToken">The token used to cancel retrieval.</param>
    /// <returns>The work-context snapshot.</returns>
    Task<WorkContextSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

/// <summary>Represents a captured Microsoft 365 work-context snapshot.</summary>
public sealed partial class WorkContextSnapshot
{
    internal WorkContextSnapshot(
        DateTimeOffset capturedAtUtc,
        WorkContextFacetResult<WorkContextUserProfile> userProfile,
        WorkContextFacetResult<WorkContextManager> manager,
        WorkContextFacetResult<WorkContextWorkSettings> workSettings,
        WorkContextFacetResult<IReadOnlyList<WorkContextCalendarEvent>> calendar)
    {
        CapturedAtUtc = capturedAtUtc;
        UserProfile = userProfile ?? throw new ArgumentNullException(nameof(userProfile));
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
        WorkSettings = workSettings ?? throw new ArgumentNullException(nameof(workSettings));
        Calendar = CopyCalendarResult(calendar ?? throw new ArgumentNullException(nameof(calendar)));
    }

    private static WorkContextFacetResult<IReadOnlyList<WorkContextCalendarEvent>> CopyCalendarResult(
        WorkContextFacetResult<IReadOnlyList<WorkContextCalendarEvent>> calendar)
    {
        if (calendar.Value is null)
        {
            return calendar;
        }

        IReadOnlyList<WorkContextCalendarEvent> copiedEvents = Array.AsReadOnly(calendar.Value.ToArray());
        return new WorkContextFacetResult<IReadOnlyList<WorkContextCalendarEvent>>(
            calendar.Status,
            copiedEvents,
            calendar.Failure);
    }

    /// <summary>Gets the UTC time at which retrieval began.</summary>
    public DateTimeOffset CapturedAtUtc { get; }

    /// <summary>Gets the signed-in user's profile result.</summary>
    public WorkContextFacetResult<WorkContextUserProfile> UserProfile { get; }

    /// <summary>Gets the signed-in user's immediate manager result.</summary>
    public WorkContextFacetResult<WorkContextManager> Manager { get; }

    /// <summary>Gets the signed-in user's work-settings result.</summary>
    public WorkContextFacetResult<WorkContextWorkSettings> WorkSettings { get; }

    /// <summary>Gets the signed-in user's calendar result.</summary>
    public WorkContextFacetResult<IReadOnlyList<WorkContextCalendarEvent>> Calendar { get; }
}