namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Contains allowlisted profile values for the signed-in user.</summary>
public sealed class WorkContextUserProfile
{
    internal WorkContextUserProfile(
        string? displayName,
        string? givenName,
        string? surname,
        string? jobTitle,
        string? department,
        string? officeLocation,
        string? preferredLanguage)
    {
        DisplayName = displayName;
        GivenName = givenName;
        Surname = surname;
        JobTitle = jobTitle;
        Department = department;
        OfficeLocation = officeLocation;
        PreferredLanguage = preferredLanguage;
    }

    /// <summary>Gets the user's display name.</summary>
    public string? DisplayName { get; }

    /// <summary>Gets the user's given name.</summary>
    public string? GivenName { get; }

    /// <summary>Gets the user's surname.</summary>
    public string? Surname { get; }

    /// <summary>Gets the user's job title.</summary>
    public string? JobTitle { get; }

    /// <summary>Gets the user's department.</summary>
    public string? Department { get; }

    /// <summary>Gets the user's office location.</summary>
    public string? OfficeLocation { get; }

    /// <summary>Gets the user's preferred language.</summary>
    public string? PreferredLanguage { get; }
}