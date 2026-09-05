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