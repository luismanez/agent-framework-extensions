using Acterion.Agents.AI.Microsoft365.Retrieval;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft365Retrieval.AspNetCore.Tests;

public sealed class SampleServiceGraphTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> factory;

    public SampleServiceGraphTests(WebApplicationFactory<Program> factory)
    {
        this.factory = factory;
    }

    [Fact]
    public void Services_ResolveTheProductionAgentAndDelegatedTokenProviderWithoutNetworkCalls()
    {
        IMicrosoft365RetrievalTokenProvider tokenProvider =
            factory.Services.GetRequiredService<IMicrosoft365RetrievalTokenProvider>();
        AIAgent agent = factory.Services.GetRequiredService<AIAgent>();

        Assert.Equal("MicrosoftIdentityWebRetrievalTokenProvider", tokenProvider.GetType().Name);
        Assert.NotNull(agent);
    }
}