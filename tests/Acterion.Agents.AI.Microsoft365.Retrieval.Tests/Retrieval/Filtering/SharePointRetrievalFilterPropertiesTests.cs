using Acterion.Agents.AI.Microsoft365.Retrieval;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests.Retrieval.Filtering;

public sealed class SharePointRetrievalFilterPropertiesTests
{
    [Theory]
    [InlineData("  Megan Bowen  ", "Author:\"Megan Bowen\"")]
    [InlineData("Adele Vance", "Author:\"Adele Vance\"")]
    public void Author_CreatesCanonicalTextTerm(string value, string expectedExpression)
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.Author(value);

        Assert.Equal(expectedExpression, filter.Expression);
    }

    [Fact]
    public void FileName_CreatesCanonicalTextTerm()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.FileName("Annual Report.pdf");

        Assert.Equal("Filename:\"Annual Report.pdf\"", filter.Expression);
    }

    [Theory]
    [InlineData("pdf", "FileType:\"pdf\"")]
    [InlineData(".PPTX", "FileType:\"pptx\"")]
    public void FileType_NormalizesCommonDeveloperInput(string value, string expectedExpression)
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.FileType(value);

        Assert.Equal(expectedExpression, filter.Expression);
    }

    [Fact]
    public void InformationProtectionLabelId_FormatsAValueAsLowercaseInvariantD()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.InformationProtectionLabelId(
            Guid.Parse("F0DDCC93-D3C0-4993-B5CC-76B0A283E252"));

        Assert.Equal(
            "InformationProtectionLabelId:\"f0ddcc93-d3c0-4993-b5cc-76b0a283e252\"",
            filter.Expression);
    }

    [Fact]
    public void ModifiedBy_CreatesCanonicalTextTerm()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.ModifiedBy("Adele Vance");

        Assert.Equal("ModifiedBy:\"Adele Vance\"", filter.Expression);
    }

    [Fact]
    public void Title_CreatesCanonicalTextTerm()
    {
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.Title("Windows 10 Device");

        Assert.Equal("Title:\"Windows 10 Device\"", filter.Expression);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Draft*")]
    [InlineData("Draft\" OR Path:*")]
    [InlineData("domain\\user")]
    [InlineData("Line 1\nLine 2")]
    public void TextProperty_UnsafeOrEmptyValue_ThrowsArgumentException(string value)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => SharePointRetrievalFilter.Title(value));

        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void TextProperty_NullValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SharePointRetrievalFilter.Author(null!));
    }

    [Fact]
    public void InformationProtectionLabelId_Empty_ThrowsArgumentException()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => SharePointRetrievalFilter.InformationProtectionLabelId(Guid.Empty));

        Assert.Equal("labelId", exception.ParamName);
    }
}