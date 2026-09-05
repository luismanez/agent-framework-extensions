using System.Reflection;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class AgentFrameworkPublicContractTests
{
    [Fact]
    public void PublicContract_ExposesOnlyTheSupportedAdapterAndBuilderOverloads()
    {
        Assert.True(typeof(Microsoft365RetrievalSearch).IsSealed);
        Assert.Single(typeof(Microsoft365RetrievalSearch).GetConstructors());

        MethodInfo searchMethod = Assert.Single(
            typeof(Microsoft365RetrievalSearch).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly));
        Assert.Equal("SearchAsync", searchMethod.Name);
        Assert.Equal(
            typeof(Task<IEnumerable<TextSearchProvider.TextSearchResult>>),
            searchMethod.ReturnType);
        Assert.Equal(
            [typeof(string), typeof(CancellationToken)],
            searchMethod.GetParameters().Select(parameter => parameter.ParameterType));

        MethodInfo[] extensionMethods = typeof(Microsoft365RetrievalChatClientBuilderExtensions)
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .OrderBy(method => method.GetParameters()[1].ParameterType.FullName)
            .ToArray();

        Assert.Equal(2, extensionMethods.Length);
        Assert.All(extensionMethods, method =>
        {
            Assert.Equal("UseMicrosoft365Retrieval", method.Name);
            Assert.Equal(typeof(ChatClientBuilder), method.ReturnType);
            Assert.Equal(typeof(ChatClientBuilder), method.GetParameters()[0].ParameterType);
        });
        Assert.Equal(
            typeof(TextSearchProviderOptions),
            extensionMethods[0].GetParameters()[1].ParameterType);
        Assert.Equal(
            typeof(TextSearchProviderOptions.TextSearchBehavior),
            extensionMethods[1].GetParameters()[1].ParameterType);
    }

    [Fact]
    public void FrameworkProbe_SupportsRequiredSearchContract()
    {
        _ = TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke;
        _ = TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling;
        TextSearchProvider.TextSearchResult result = new() { RawRepresentation = new object() };

        Assert.NotNull(result.RawRepresentation);
    }

    private static void CompileConsumerUsage(
        ChatClientBuilder builder,
        Microsoft365RetrievalSearch search,
        TextSearchProviderOptions options)
    {
        Func<string, CancellationToken, Task<IEnumerable<TextSearchProvider.TextSearchResult>>> searchDelegate =
            search.SearchAsync;

        _ = builder.UseMicrosoft365Retrieval(TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling);
        _ = builder.UseMicrosoft365Retrieval(options);
        _ = searchDelegate;
    }
}