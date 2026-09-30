namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Identifies a supported Microsoft 365 Copilot Retrieval API data source.
/// </summary>
public enum Microsoft365RetrievalDataSource
{
    /// <summary>
    /// Retrieves content from SharePoint.
    /// </summary>
    SharePoint = 0,

    /// <summary>
    /// Retrieves content from organizational OneDrive.
    /// </summary>
    OneDriveBusiness = 1,
}
