using System.Globalization;

namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Creates retrieval constraints from trusted SharePoint paths and site identifiers.
/// Generated filters narrow retrieval but do not provide authorization.
/// </summary>
public sealed class SharePointRetrievalFilter
{
    private readonly string[] terms;

    private SharePointRetrievalFilter(IEnumerable<string> terms, string expression)
    {
        this.terms = terms.ToArray();
        Expression = expression;
    }

    /// <summary>
    /// Gets the canonical filter expression for assignment to retrieval options.
    /// </summary>
    public string Expression { get; }

    /// <summary>
    /// Creates a filter for an absolute HTTPS SharePoint path.
    /// </summary>
    /// <param name="path">A trusted absolute HTTPS SharePoint path.</param>
    /// <returns>A filter that narrows retrieval to the supplied path.</returns>
    public static SharePointRetrievalFilter Path(Uri path)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!path.IsAbsoluteUri ||
            !string.Equals(path.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(path.UserInfo) ||
            !string.IsNullOrEmpty(path.Query) ||
            !string.IsNullOrEmpty(path.Fragment))
        {
            throw new ArgumentException(
                "Path must be an absolute HTTPS URI without user information, a query, or a fragment.",
                nameof(path));
        }

        string term = $"Path:\"{path.AbsoluteUri}\"";
        return new SharePointRetrievalFilter([term], term);
    }

    /// <summary>
    /// Creates a filter for a SharePoint site collection identifier.
    /// </summary>
    /// <param name="siteId">A non-empty SharePoint site collection identifier.</param>
    /// <returns>A filter that narrows retrieval to the supplied site identifier.</returns>
    public static SharePointRetrievalFilter SiteId(Guid siteId)
    {
        if (siteId == Guid.Empty)
        {
            throw new ArgumentException("Site ID must not be empty.", nameof(siteId));
        }

        string term = $"SiteID:\"{siteId.ToString("D", CultureInfo.InvariantCulture).ToLowerInvariant()}\"";
        return new SharePointRetrievalFilter([term], term);
    }

    /// <summary>
    /// Combines trusted filters with an ordered logical OR expression.
    /// </summary>
    /// <param name="filters">One or more filters to combine.</param>
    /// <returns>A filter preserving the supplied terms, order, and duplicates.</returns>
    public static SharePointRetrievalFilter AnyOf(params SharePointRetrievalFilter[] filters)
    {
        ArgumentNullException.ThrowIfNull(filters);

        if (filters.Length == 0)
        {
            throw new ArgumentException("At least one filter is required.", nameof(filters));
        }

        if (filters.Any(filter => filter is null))
        {
            throw new ArgumentException("Filters cannot contain null elements.", nameof(filters));
        }

        string[] terms = filters.SelectMany(filter => filter.terms).ToArray();
        string expression = terms.Length == 1 ? terms[0] : $"({string.Join(" OR ", terms)})";

        return new SharePointRetrievalFilter(terms, expression);
    }
}