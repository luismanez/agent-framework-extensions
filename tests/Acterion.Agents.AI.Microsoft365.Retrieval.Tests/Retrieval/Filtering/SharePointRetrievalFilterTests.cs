using Acterion.Agents.AI.Microsoft365.Retrieval;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests.Retrieval.Filtering;

public sealed partial class SharePointRetrievalFilterTests
{
    [Theory]
    [InlineData("https://contoso.sharepoint.com/sites/engineering/", "Path:\"https://contoso.sharepoint.com/sites/engineering/\"")]
    [InlineData("https://contoso.sharepoint.com/sites/engineering", "Path:\"https://contoso.sharepoint.com/sites/engineering\"")]
    [InlineData("https://contoso.sharepoint.com/sites/engineering/Policies/Remote%20Work.docx", "Path:\"https://contoso.sharepoint.com/sites/engineering/Policies/Remote%20Work.docx\"")]
    [InlineData("https://contoso.sharepoint.com/sites/%E6%9D%B1%E4%BA%AC/", "Path:\"https://contoso.sharepoint.com/sites/%E6%9D%B1%E4%BA%AC/\"")]
    public void Path_UsesTheEscapedAbsoluteUriWithoutChangingPathSemantics(string value, string expectedExpression)
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.Path(new Uri(value));

        Assert.Equal(expectedExpression, filter.Expression);
    }

    [Fact]
    public void Path_PreservesPercentEncodedKqlLookingData()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.Path(
            new Uri("https://contoso.sharepoint.com/sites/engineering/%22%20OR%20SiteID%3A%22not-a-filter"));

        Assert.Equal(
            "Path:\"https://contoso.sharepoint.com/sites/engineering/%22%20OR%20SiteID%3A%22not-a-filter\"",
            filter.Expression);
        Assert.DoesNotContain("\" OR SiteID:\"not-a-filter", filter.Expression, StringComparison.Ordinal);
    }

    [Fact]
    public void Path_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SharePointRetrievalFilter.Path(null!));
    }

    [Theory]
    [InlineData("/sites/engineering", UriKind.Relative)]
    [InlineData("http://contoso.sharepoint.com/sites/engineering", UriKind.Absolute)]
    [InlineData("https://user@contoso.sharepoint.com/sites/engineering", UriKind.Absolute)]
    [InlineData("https://contoso.sharepoint.com/sites/engineering?view=all", UriKind.Absolute)]
    [InlineData("https://contoso.sharepoint.com/sites/engineering#policies", UriKind.Absolute)]
    public void Path_InvalidUri_ThrowsArgumentException(string value, UriKind uriKind)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => SharePointRetrievalFilter.Path(new Uri(value, uriKind)));

        Assert.Equal("path", exception.ParamName);
    }

    [Fact]
    public void SiteId_FormatsAValueAsLowercaseInvariantD()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.SiteId(
            Guid.Parse("F9A9F9BC-5D23-4ED4-A960-05BA6A83BDB6"));

        Assert.Equal("SiteID:\"f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6\"", filter.Expression);
    }

    [Fact]
    public void SiteId_Empty_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => SharePointRetrievalFilter.SiteId(Guid.Empty));

        Assert.Equal("siteId", exception.ParamName);
    }

    [Theory]
    [InlineData("pdf", "FileExtension:\"pdf\"")]
    [InlineData(".DOCX", "FileExtension:\"docx\"")]
    public void FileExtension_NormalizesCommonDeveloperInput(string value, string expectedExpression)
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.FileExtension(value);

        Assert.Equal(expectedExpression, filter.Expression);
    }

    [Fact]
    public void FileExtensions_CombinesExtensionsWithOr()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.FileExtensions("pdf", ".DOCX");

        Assert.Equal(
            "(FileExtension:\"pdf\" OR FileExtension:\"docx\")",
            filter.Expression);
    }

    [Fact]
    public void FileExtensions_SingleExtension_ReturnsItsExpression()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.FileExtensions("pdf");

        Assert.Equal("FileExtension:\"pdf\"", filter.Expression);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("  pdf  ")]
    [InlineData("tar.gz")]
    [InlineData("pdf*")]
    public void FileExtension_InvalidValue_ThrowsArgumentException(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => SharePointRetrievalFilter.FileExtension(value));

        Assert.Equal("extension", exception.ParamName);
    }

    [Fact]
    public void FileExtensions_NullArray_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SharePointRetrievalFilter.FileExtensions(null!));
    }

    [Fact]
    public void FileExtensions_EmptyArray_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => SharePointRetrievalFilter.FileExtensions([]));

        Assert.Equal("extensions", exception.ParamName);
    }

    [Fact]
    public void FileExtensions_NullElement_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => SharePointRetrievalFilter.FileExtensions(["pdf", null!]));

        Assert.Equal("extensions", exception.ParamName);
    }

    [Fact]
    public void LastModifiedOnOrAfter_FormatsTheInstantAsUtc()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.LastModifiedOnOrAfter(
            new DateTimeOffset(2026, 9, 8, 15, 30, 45, TimeSpan.FromHours(2)));

        Assert.Equal("LastModifiedTime>=2026-09-08T13:30:45Z", filter.Expression);
    }

    [Fact]
    public void LastModifiedOnOrBefore_PreservesFractionalSecondPrecision()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.LastModifiedOnOrBefore(
            new DateTimeOffset(2026, 9, 8, 13, 30, 45, 123, TimeSpan.Zero));

        Assert.Equal("LastModifiedTime<=2026-09-08T13:30:45.123Z", filter.Expression);
    }

    [Fact]
    public void LastModifiedBetween_CreatesInclusiveUtcBounds()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.LastModifiedBetween(
            new DateTimeOffset(2026, 1, 1, 2, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2026, 6, 30, 20, 0, 0, TimeSpan.FromHours(-4)));

        Assert.Equal(
            "(LastModifiedTime>=2026-01-01T00:00:00Z AND " +
            "LastModifiedTime<=2026-07-01T00:00:00Z)",
            filter.Expression);
    }

    [Fact]
    public void LastModifiedBetween_ReversedRange_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            SharePointRetrievalFilter.LastModifiedBetween(
                new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));

        Assert.Equal("from", exception.ParamName);
    }

    [Fact]
    public void LastModifiedBetween_EqualBounds_AreAllowed()
    {
        DateTimeOffset instant = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        SharePointRetrievalFilter filter = SharePointRetrievalFilter.LastModifiedBetween(instant, instant);

        Assert.Equal(
            "(LastModifiedTime>=2026-01-01T00:00:00Z AND " +
            "LastModifiedTime<=2026-01-01T00:00:00Z)",
            filter.Expression);
    }

    [Fact]
    public void AnyOf_SingleFilter_ReturnsItsExpression()
    {
        SharePointRetrievalFilter path = SharePointRetrievalFilter.Path(
            new Uri("https://contoso.sharepoint.com/sites/engineering/"));

        SharePointRetrievalFilter filter = SharePointRetrievalFilter.AnyOf(path);

        Assert.Equal(path.Expression, filter.Expression);
    }

    [Fact]
    public void AnyOf_MultipleAndNestedFilters_FlattensTermsInInputOrder()
    {
        SharePointRetrievalFilter path = SharePointRetrievalFilter.Path(
            new Uri("https://contoso.sharepoint.com/sites/engineering/"));
        SharePointRetrievalFilter firstSite = SharePointRetrievalFilter.SiteId(
            Guid.Parse("f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6"));
        SharePointRetrievalFilter secondSite = SharePointRetrievalFilter.SiteId(
            Guid.Parse("d87162e0-d26d-4c8f-84f1-d4a14a871e02"));

        SharePointRetrievalFilter filter = SharePointRetrievalFilter.AnyOf(
            path,
            SharePointRetrievalFilter.AnyOf(firstSite, secondSite),
            path);

        Assert.Equal(
            "(Path:\"https://contoso.sharepoint.com/sites/engineering/\" OR " +
            "SiteID:\"f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6\" OR " +
            "SiteID:\"d87162e0-d26d-4c8f-84f1-d4a14a871e02\" OR " +
            "Path:\"https://contoso.sharepoint.com/sites/engineering/\")",
            filter.Expression);
    }

    [Fact]
    public void AllOf_MixedWithAnyOf_PreservesLogicalGrouping()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.AllOf(
            SharePointRetrievalFilter.SiteId(
                Guid.Parse("f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6")),
            SharePointRetrievalFilter.FileExtensions("pdf", "docx"),
            SharePointRetrievalFilter.LastModifiedOnOrAfter(
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));

        Assert.Equal(
            "(SiteID:\"f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6\" AND " +
            "(FileExtension:\"pdf\" OR FileExtension:\"docx\") AND " +
            "LastModifiedTime>=2026-01-01T00:00:00Z)",
            filter.Expression);
    }

    [Fact]
    public void AllOf_NestedFilters_FlattensOnlyAndExpressions()
    {
        SharePointRetrievalFilter site = SharePointRetrievalFilter.SiteId(
            Guid.Parse("f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6"));
        SharePointRetrievalFilter extensions = SharePointRetrievalFilter.FileExtensions("pdf", "docx");
        SharePointRetrievalFilter modified = SharePointRetrievalFilter.LastModifiedOnOrAfter(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        SharePointRetrievalFilter filter = SharePointRetrievalFilter.AllOf(
            site,
            SharePointRetrievalFilter.AllOf(extensions, modified));

        Assert.Equal(
            "(SiteID:\"f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6\" AND " +
            "(FileExtension:\"pdf\" OR FileExtension:\"docx\") AND " +
            "LastModifiedTime>=2026-01-01T00:00:00Z)",
            filter.Expression);
    }

    [Fact]
    public void Not_PreservesTheOperandAsAGroup()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.Not(
            SharePointRetrievalFilter.FileExtensions("aspx", "html"));

        Assert.Equal(
            "NOT (FileExtension:\"aspx\" OR FileExtension:\"html\")",
            filter.Expression);
    }

    [Fact]
    public void Not_NullFilter_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SharePointRetrievalFilter.Not(null!));
    }

    [Fact]
    public void Not_NestedNot_PreservesLogicalGrouping()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.Not(
            SharePointRetrievalFilter.Not(
                SharePointRetrievalFilter.FileExtension("pdf")));

        Assert.Equal("NOT (NOT (FileExtension:\"pdf\"))", filter.Expression);
    }

    [Fact]
    public void AllOf_EmptyArray_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => SharePointRetrievalFilter.AllOf([]));

        Assert.Equal("filters", exception.ParamName);
    }

    [Fact]
    public void AllOf_NullArray_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SharePointRetrievalFilter.AllOf(null!));
    }

    [Fact]
    public void AllOf_NullElement_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => SharePointRetrievalFilter.AllOf([null!]));

        Assert.Equal("filters", exception.ParamName);
    }

    [Fact]
    public void AnyOf_NullArray_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SharePointRetrievalFilter.AnyOf(null!));
    }

    [Fact]
    public void AnyOf_EmptyArray_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => SharePointRetrievalFilter.AnyOf([]));

        Assert.Equal("filters", exception.ParamName);
    }

    [Fact]
    public void AnyOf_NullElement_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => SharePointRetrievalFilter.AnyOf([null!]));

        Assert.Equal("filters", exception.ParamName);
    }

    [Fact]
    public void AnyOf_SnapshotsTheInputArray()
    {
        SharePointRetrievalFilter[] filters =
        [
            SharePointRetrievalFilter.Path(new Uri("https://contoso.sharepoint.com/sites/engineering/")),
            SharePointRetrievalFilter.SiteId(Guid.Parse("f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6")),
        ];

        SharePointRetrievalFilter filter = SharePointRetrievalFilter.AnyOf(filters);
        filters[0] = SharePointRetrievalFilter.SiteId(Guid.Parse("d87162e0-d26d-4c8f-84f1-d4a14a871e02"));

        Assert.Equal(
            "(Path:\"https://contoso.sharepoint.com/sites/engineering/\" OR " +
            "SiteID:\"f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6\")",
            filter.Expression);
    }
}