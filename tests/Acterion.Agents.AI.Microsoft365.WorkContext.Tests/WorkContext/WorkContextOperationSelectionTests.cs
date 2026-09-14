using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class WorkContextOperationSelectionTests
{
    [Theory]
    [InlineData(0, "")]
    [InlineData(1, "profile")]
    [InlineData(2, "manager")]
    [InlineData(3, "profile,manager")]
    [InlineData(4, "work-time-zone,work-language,work-hours")]
    [InlineData(5, "profile,work-time-zone,work-language,work-hours")]
    [InlineData(6, "manager,work-time-zone,work-language,work-hours")]
    [InlineData(7, "profile,manager,work-time-zone,work-language,work-hours")]
    [InlineData(8, "calendar")]
    [InlineData(9, "profile,calendar")]
    [InlineData(10, "manager,calendar")]
    [InlineData(11, "profile,manager,calendar")]
    [InlineData(12, "work-time-zone,work-language,work-hours,calendar")]
    [InlineData(13, "profile,work-time-zone,work-language,work-hours,calendar")]
    [InlineData(14, "manager,work-time-zone,work-language,work-hours,calendar")]
    [InlineData(15, "profile,manager,work-time-zone,work-language,work-hours,calendar")]
    public void Select_ReturnsRequiredOperationsInStableOrder(int enabledFacets, string expectedIds)
    {
        Microsoft365WorkContextOptions options = new()
        {
            EnableUserProfile = (enabledFacets & 1) != 0,
            EnableManager = (enabledFacets & 2) != 0,
            EnableWorkSettings = (enabledFacets & 4) != 0,
            EnableCalendar = (enabledFacets & 8) != 0,
        };
        Microsoft365WorkContextOptionsSnapshot snapshot =
            Microsoft365WorkContextOptionsValidator.ValidateAndSnapshot(options);

        IReadOnlyList<MicrosoftGraphOperation> operations = MicrosoftGraphOperations.Select(
            snapshot,
            new DateTimeOffset(2026, 9, 13, 12, 34, 56, TimeSpan.Zero));

        string[] expected = expectedIds.Length == 0 ? [] : expectedIds.Split(',');
        Assert.Equal(expected, operations.Select(operation => operation.Id));
        Assert.Equal(operations.Count, operations.Select(operation => operation.Id).Distinct().Count());
        Assert.All(operations, operation => Assert.StartsWith("v1.0/", operation.DirectUri.OriginalString));
        Assert.All(operations, operation => Assert.StartsWith("/me", operation.BatchUrl));
        Assert.All(
            operations.Where(operation => operation.Id.StartsWith("work-", StringComparison.Ordinal)),
            operation => Assert.Equal(WorkContextFacet.WorkSettings, operation.Facet));
    }
}