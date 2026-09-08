namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Describes the Microsoft Purview sensitivity label applied to a retrieval result.
/// </summary>
public sealed class Microsoft365RetrievalSensitivityLabel
{
    internal Microsoft365RetrievalSensitivityLabel(
        string? sensitivityLabelId,
        string? displayName,
        string? toolTip,
        int? priority,
        string? color)
    {
        SensitivityLabelId = sensitivityLabelId;
        DisplayName = displayName;
        ToolTip = toolTip;
        Priority = priority;
        Color = color;
    }

    /// <summary>
    /// Gets the sensitivity label identifier.
    /// </summary>
    public string? SensitivityLabelId { get; }

    /// <summary>
    /// Gets the display name of the sensitivity label.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the user-facing description of the sensitivity label.
    /// </summary>
    public string? ToolTip { get; }

    /// <summary>
    /// Gets the sensitivity label priority.
    /// </summary>
    public int? Priority { get; }

    /// <summary>
    /// Gets the display color associated with the sensitivity label.
    /// </summary>
    public string? Color { get; }
}