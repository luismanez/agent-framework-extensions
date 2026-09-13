namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Contains a mailbox locale identifier and display name.</summary>
public sealed class WorkContextLocale
{
    internal WorkContextLocale(string? locale, string? displayName)
    {
        Locale = locale;
        DisplayName = displayName;
    }

    /// <summary>Gets the locale identifier.</summary>
    public string? Locale { get; }

    /// <summary>Gets the locale display name.</summary>
    public string? DisplayName { get; }
}