namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Contains allowlisted values for the signed-in user's immediate manager.</summary>
public sealed class WorkContextManager
{
    internal WorkContextManager(
        string? displayName,
        string? jobTitle,
        string? department,
        string? officeLocation)
    {
        DisplayName = displayName;
        JobTitle = jobTitle;
        Department = department;
        OfficeLocation = officeLocation;
    }

    /// <summary>Gets the manager's display name.</summary>
    public string? DisplayName { get; }

    /// <summary>Gets the manager's job title.</summary>
    public string? JobTitle { get; }

    /// <summary>Gets the manager's department.</summary>
    public string? Department { get; }

    /// <summary>Gets the manager's office location.</summary>
    public string? OfficeLocation { get; }
}