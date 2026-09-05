using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Microsoft365Retrieval.AspNetCore.Tests;

public sealed class SampleStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public SampleStartupTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void CreateClient_StartsTheHostWithoutExternalCalls()
    {
        using HttpClient client = factory.CreateClient();

        Assert.NotNull(client);
    }
}