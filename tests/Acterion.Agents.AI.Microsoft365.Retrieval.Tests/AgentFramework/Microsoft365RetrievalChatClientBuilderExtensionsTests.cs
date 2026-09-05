using Acterion.Agents.AI.Microsoft365.Retrieval;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class Microsoft365RetrievalChatClientBuilderExtensionsTests
{
    [Fact]
    public void UseMicrosoft365Retrieval_DefersRequiredServiceResolutionUntilBuild()
    {
        ChatClientBuilder builder = new(_ => new StubChatClient());

        ChatClientBuilder configuredBuilder = builder.UseMicrosoft365Retrieval(
            TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling);

        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => configuredBuilder.Build(services));

        Assert.Contains(nameof(Microsoft365RetrievalSearch), exception.Message);
    }

    [Fact]
    public void UseMicrosoft365Retrieval_RejectsNullArgumentsSynchronously()
    {
        Assert.Throws<ArgumentNullException>(
            () => Microsoft365RetrievalChatClientBuilderExtensions.UseMicrosoft365Retrieval(
                null!,
                new TextSearchProviderOptions()));

        ChatClientBuilder builder = new(_ => new StubChatClient());

        Assert.Throws<ArgumentNullException>(
            () => builder.UseMicrosoft365Retrieval((TextSearchProviderOptions)null!));
    }

    [Fact]
    public void AddMicrosoft365Retrieval_RegistersAnIsolatedTransientSearchAdapter()
    {
        ServiceCollection services = new();
        services.AddSingleton<IMicrosoft365RetrievalTokenProvider>(new StubTokenProvider());
        services.AddMicrosoft365Retrieval(_ => { });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        Microsoft365RetrievalSearch firstSearch =
            serviceProvider.GetRequiredService<Microsoft365RetrievalSearch>();
        Microsoft365RetrievalSearch secondSearch =
            serviceProvider.GetRequiredService<Microsoft365RetrievalSearch>();

        Assert.NotSame(firstSearch, secondSearch);
    }

    [Theory]
    [InlineData(TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke)]
    [InlineData(TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling)]
    public void UseMicrosoft365Retrieval_BuildsTheFullContextProviderPipeline(
        TextSearchProviderOptions.TextSearchBehavior behavior)
    {
        ServiceCollection services = new();
        services.AddSingleton<IMicrosoft365RetrievalTokenProvider>(new StubTokenProvider());
        services.AddMicrosoft365Retrieval(_ => { });
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        ChatClientBuilder builder = new(_ => new StubChatClient());

        IChatClient client = builder.UseMicrosoft365Retrieval(behavior).Build(serviceProvider);

        Assert.NotNull(client);
    }

    [Fact]
    public async Task UseMicrosoft365Retrieval_PreservesSurroundingPipelineStages()
    {
        List<string> events = [];
        StubChatClient innerClient = new((_, _) =>
        {
            events.Add("model");
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "Done."));
        });
        ServiceCollection services = new();
        services.AddSingleton<IMicrosoft365RetrievalTokenProvider>(new StubTokenProvider());
        services.AddMicrosoft365Retrieval(_ => { });
        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        ChatClientBuilder builder = new(_ => innerClient);
        builder.Use((client, _) => new RecordingChatClient(client, "before", events));
        builder.UseMicrosoft365Retrieval(TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke);
        builder.Use((client, _) => new RecordingChatClient(client, "after", events));
        AIAgent agent = builder.Build(serviceProvider).AsAIAgent(
            new ChatClientAgentOptions(),
            services: serviceProvider);

        _ = await agent.RunAsync(
            [new ChatMessage(ChatRole.User, "Find the project plan")],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, events.Count(@event => @event == "before"));
        Assert.Equal(1, events.Count(@event => @event == "after"));
        Assert.True(events.IndexOf("before") < events.IndexOf("model"));
        Assert.True(events.IndexOf("after") < events.IndexOf("model"));
    }

    private sealed class RecordingChatClient(IChatClient innerClient, string name, List<string> events)
        : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            events.Add(name);
            return innerClient.GetResponseAsync(messages, options, cancellationToken);
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            events.Add(name);

            await foreach (ChatResponseUpdate update in innerClient.GetStreamingResponseAsync(
                messages,
                options,
                cancellationToken))
            {
                yield return update;
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            innerClient.GetService(serviceType, serviceKey);

        public void Dispose() => innerClient.Dispose();
    }
}