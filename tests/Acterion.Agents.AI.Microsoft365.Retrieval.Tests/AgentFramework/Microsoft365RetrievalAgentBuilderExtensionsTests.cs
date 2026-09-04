using Acterion.Agents.AI.Microsoft365.Retrieval;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class Microsoft365RetrievalAgentBuilderExtensionsTests
{
    [Fact]
    public void UseMicrosoft365Retrieval_DefersRequiredServiceResolutionUntilBuild()
    {
        AIAgentBuilder builder = new(_ => null!);

        AIAgentBuilder configuredBuilder = builder.UseMicrosoft365Retrieval(
            TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke);

        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => configuredBuilder.Build(services));

        Assert.Contains(nameof(Microsoft365RetrievalSearch), exception.Message);
    }

    [Fact]
    public void UseMicrosoft365Retrieval_RejectsNullArgumentsSynchronously()
    {
        Assert.Throws<ArgumentNullException>(
            () => Microsoft365RetrievalAgentBuilderExtensions.UseMicrosoft365Retrieval(
                null!,
                new TextSearchProviderOptions()));

        AIAgentBuilder builder = new(_ => null!);

        Assert.Throws<ArgumentNullException>(
            () => builder.UseMicrosoft365Retrieval((TextSearchProviderOptions)null!));
    }
}