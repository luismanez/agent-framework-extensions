using System.Globalization;

namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Creates retrieval constraints from trusted values for supported SharePoint properties.
/// Generated filters narrow retrieval but do not provide authorization.
/// </summary>
public sealed class SharePointRetrievalFilter
{
    private readonly CompositionKind compositionKind;
    private readonly string[] operands;

    private SharePointRetrievalFilter(
        IEnumerable<string> operands,
        string expression,
        CompositionKind compositionKind = CompositionKind.Term)
    {
        this.operands = operands.ToArray();
        this.compositionKind = compositionKind;
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

        string term = $"SiteID:\"{FormatGuid(siteId)}\"";
        return new SharePointRetrievalFilter([term], term);
    }

    /// <summary>
    /// Creates a filter for a file extension.
    /// </summary>
    /// <param name="extension">A file extension, with or without a leading period.</param>
    /// <returns>A filter that narrows retrieval to the supplied file extension.</returns>
    public static SharePointRetrievalFilter FileExtension(string extension)
    {
        return CreateFileClassificationFilter("FileExtension", extension, nameof(extension));
    }

    /// <summary>
    /// Creates a filter matching any of the supplied file extensions.
    /// </summary>
    /// <param name="extensions">One or more file extensions, with or without leading periods.</param>
    /// <returns>A filter that narrows retrieval to any supplied file extension.</returns>
    public static SharePointRetrievalFilter FileExtensions(params string[] extensions)
    {
        ArgumentNullException.ThrowIfNull(extensions);

        if (extensions.Length == 0)
        {
            throw new ArgumentException("At least one file extension is required.", nameof(extensions));
        }

        if (extensions.Any(extension => extension is null))
        {
            throw new ArgumentException("File extensions cannot contain null elements.", nameof(extensions));
        }

        return AnyOf(extensions.Select(FileExtension).ToArray());
    }

    /// <summary>
    /// Creates a filter for content authored by the supplied person.
    /// </summary>
    /// <param name="author">The author value to match.</param>
    /// <returns>A filter that narrows retrieval by author.</returns>
    public static SharePointRetrievalFilter Author(string author)
    {
        return CreateTextFilter("Author", author, nameof(author));
    }

    /// <summary>
    /// Creates a filter for a file name.
    /// </summary>
    /// <param name="fileName">The file name to match, including its extension when applicable.</param>
    /// <returns>A filter that narrows retrieval by file name.</returns>
    public static SharePointRetrievalFilter FileName(string fileName)
    {
        return CreateTextFilter("Filename", fileName, nameof(fileName));
    }

    /// <summary>
    /// Creates a filter for a SharePoint file type.
    /// </summary>
    /// <param name="fileType">A file type, with or without a leading period.</param>
    /// <returns>A filter that narrows retrieval to the supplied file type.</returns>
    public static SharePointRetrievalFilter FileType(string fileType)
    {
        return CreateFileClassificationFilter("FileType", fileType, nameof(fileType));
    }

    /// <summary>
    /// Creates a filter for an information protection label identifier.
    /// </summary>
    /// <param name="labelId">A non-empty information protection label identifier.</param>
    /// <returns>A filter that narrows retrieval to content with the supplied label.</returns>
    public static SharePointRetrievalFilter InformationProtectionLabelId(Guid labelId)
    {
        if (labelId == Guid.Empty)
        {
            throw new ArgumentException("Information protection label ID must not be empty.", nameof(labelId));
        }

        string term = $"InformationProtectionLabelId:\"{FormatGuid(labelId)}\"";
        return new SharePointRetrievalFilter([term], term);
    }

    /// <summary>
    /// Creates a filter for content modified by the supplied person.
    /// </summary>
    /// <param name="modifiedBy">The modified-by value to match.</param>
    /// <returns>A filter that narrows retrieval by the person who modified the content.</returns>
    public static SharePointRetrievalFilter ModifiedBy(string modifiedBy)
    {
        return CreateTextFilter("ModifiedBy", modifiedBy, nameof(modifiedBy));
    }

    /// <summary>
    /// Creates a filter for a title phrase.
    /// </summary>
    /// <param name="title">The title phrase to match.</param>
    /// <returns>A filter that narrows retrieval by title.</returns>
    public static SharePointRetrievalFilter Title(string title)
    {
        return CreateTextFilter("Title", title, nameof(title));
    }

    /// <summary>
    /// Creates an inclusive lower-bound filter for the last modified time.
    /// </summary>
    /// <param name="value">The earliest accepted modification instant.</param>
    /// <returns>A filter that includes content modified at or after the supplied instant.</returns>
    public static SharePointRetrievalFilter LastModifiedOnOrAfter(DateTimeOffset value)
    {
        string term = $"LastModifiedTime>={FormatUtc(value)}";
        return new SharePointRetrievalFilter([term], term);
    }

    /// <summary>
    /// Creates an inclusive upper-bound filter for the last modified time.
    /// </summary>
    /// <param name="value">The latest accepted modification instant.</param>
    /// <returns>A filter that includes content modified at or before the supplied instant.</returns>
    public static SharePointRetrievalFilter LastModifiedOnOrBefore(DateTimeOffset value)
    {
        string term = $"LastModifiedTime<={FormatUtc(value)}";
        return new SharePointRetrievalFilter([term], term);
    }

    /// <summary>
    /// Creates an inclusive filter for a last modified time range.
    /// </summary>
    /// <param name="from">The earliest accepted modification instant.</param>
    /// <param name="to">The latest accepted modification instant.</param>
    /// <returns>A filter that includes content modified within the supplied range.</returns>
    public static SharePointRetrievalFilter LastModifiedBetween(DateTimeOffset from, DateTimeOffset to)
    {
        if (from > to)
        {
            throw new ArgumentException(
                "The start of the last modified range must not be after its end.",
                nameof(from));
        }

        return AllOf(LastModifiedOnOrAfter(from), LastModifiedOnOrBefore(to));
    }

    /// <summary>
    /// Combines trusted filters with an ordered logical OR expression.
    /// </summary>
    /// <param name="filters">One or more filters to combine.</param>
    /// <returns>A filter preserving the supplied terms, order, and duplicates.</returns>
    public static SharePointRetrievalFilter AnyOf(params SharePointRetrievalFilter[] filters)
    {
        return Combine(CompositionKind.Or, "OR", filters);
    }

    /// <summary>
    /// Combines trusted filters with an ordered logical AND expression.
    /// </summary>
    /// <param name="filters">One or more filters to combine.</param>
    /// <returns>A filter preserving the supplied filters, order, and duplicates.</returns>
    public static SharePointRetrievalFilter AllOf(params SharePointRetrievalFilter[] filters)
    {
        return Combine(CompositionKind.And, "AND", filters);
    }

    /// <summary>
    /// Negates a trusted filter.
    /// </summary>
    /// <param name="filter">The filter to negate.</param>
    /// <returns>A filter that excludes content matching the supplied filter.</returns>
    public static SharePointRetrievalFilter Not(SharePointRetrievalFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        string groupedOperand = filter.compositionKind is CompositionKind.And or CompositionKind.Or
            ? filter.Expression
            : $"({filter.Expression})";
        string expression = $"NOT {groupedOperand}";
        return new SharePointRetrievalFilter([filter.Expression], expression, CompositionKind.Not);
    }

    private static SharePointRetrievalFilter Combine(
        CompositionKind compositionKind,
        string operatorText,
        SharePointRetrievalFilter[] filters)
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

        if (filters.Length == 1)
        {
            SharePointRetrievalFilter filter = filters[0];
            return new SharePointRetrievalFilter(
                filter.operands,
                filter.Expression,
                filter.compositionKind);
        }

        string[] operands = filters
            .SelectMany(filter => filter.compositionKind == compositionKind
                ? filter.operands
                : [filter.Expression])
            .ToArray();
        string expression = $"({string.Join($" {operatorText} ", operands)})";

        return new SharePointRetrievalFilter(operands, expression, compositionKind);
    }

    private static SharePointRetrievalFilter CreateFileClassificationFilter(
        string propertyName,
        string value,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        string normalizedValue = value.StartsWith(".", StringComparison.Ordinal)
            ? value[1..]
            : value;

        if (string.IsNullOrWhiteSpace(normalizedValue) ||
            !normalizedValue.All(char.IsAsciiLetterOrDigit))
        {
            throw new ArgumentException(
                "File classification must contain only ASCII letters or digits.",
                parameterName);
        }

        string term = $"{propertyName}:\"{normalizedValue.ToLowerInvariant()}\"";
        return new SharePointRetrievalFilter([term], term);
    }

    private static SharePointRetrievalFilter CreateTextFilter(
        string propertyName,
        string value,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        string normalizedValue = value.Trim();
        if (normalizedValue.Length == 0 ||
            value.Any(char.IsControl) ||
            normalizedValue.IndexOfAny(['"', '\\', '*']) >= 0)
        {
            throw new ArgumentException(
                "Filter value must not be empty or contain quotation marks, backslashes, wildcards, or control characters.",
                parameterName);
        }

        string term = $"{propertyName}:\"{normalizedValue}\"";
        return new SharePointRetrievalFilter([term], term);
    }

    private static string FormatGuid(Guid value)
    {
        return value.ToString("D", CultureInfo.InvariantCulture).ToLowerInvariant();
    }

    private static string FormatUtc(DateTimeOffset value)
    {
        DateTimeOffset utc = value.ToUniversalTime();
        string seconds = utc.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        long fractionalTicks = utc.Ticks % TimeSpan.TicksPerSecond;

        if (fractionalTicks == 0)
        {
            return $"{seconds}Z";
        }

        string fraction = fractionalTicks
            .ToString("D7", CultureInfo.InvariantCulture)
            .TrimEnd('0');
        return $"{seconds}.{fraction}Z";
    }

    private enum CompositionKind
    {
        Term,
        And,
        Or,
        Not,
    }
}