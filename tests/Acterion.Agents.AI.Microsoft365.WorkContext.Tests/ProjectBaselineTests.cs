using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests;

public sealed class ProjectBaselineTests
{
    [Fact]
    public void TestAssembly_Loads()
    {
        Assert.NotNull(typeof(ProjectBaselineTests).Assembly);
    }
}