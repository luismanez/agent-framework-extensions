using System.Reflection;
using System.Text.Json;
using Acterion.Agents.AI.Microsoft365.Retrieval;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class Microsoft365RetrievalAgentBehaviorTests
{
    [Fact]
    public async Task BeforeAIInvoke_RetrievesBeforeInvokingTheInnerAgent()
    {
        List<string> events = [];
        StubChatClient innerClient = new((_, _) =>
        {
            events.Add("model");
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "Found the project plan."));
        });
        StubRetrievalClient retrievalClient = new(events, [CreateHit()]);
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton(new Microsoft365RetrievalSearch(retrievalClient))
            .BuildServiceProvider();
        IChatClient client = new ChatClientBuilder(_ => innerClient)
            .UseMicrosoft365Retrieval(TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke)
            .Build(services);
        AIAgent agent = client.AsAIAgent(new ChatClientAgentOptions(), services: services);

        _ = await agent.RunAsync(
            [new ChatMessage(ChatRole.User, "Find the project plan")],
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["retrieval", "model"], events);
        ChatMessage context = Assert.Single(
            innerClient.LastMessages,
            message => message.Role == ChatRole.User && message.Text != "Find the project plan");
        Assert.Contains("Project plan", context.Text);
        Assert.Contains("https://contoso.sharepoint.com/sites/projects/plan.docx", context.Text);
        Assert.Contains("The project plan content.", context.Text);
        Assert.DoesNotContain(
            innerClient.LastMessages,
            message => message.Role == ChatRole.System && message.Text.Contains("The project plan content."));
    }

    [Fact]
    public async Task OnDemandFunctionCalling_AdvertisesAndInvokesTheSearchTool()
    {
        List<string> events = [];
        StubRetrievalClient retrievalClient = new(events, [CreateHit()]);
        StubChatClient innerClient = new((callCount, options) =>
        {
            events.Add("model");

            if (callCount == 1)
            {
                AIFunction searchTool = Assert.Single(options!.Tools!.OfType<AIFunction>());
                JsonElement schema = Assert.IsType<JsonElement>(searchTool.JsonSchema);
                string argumentName = schema.GetProperty("properties").EnumerateObject().Single().Name;

                return new ChatResponse(new ChatMessage(
                    ChatRole.Assistant,
                    [new FunctionCallContent(
                        "call-1",
                        searchTool.Name,
                        new Dictionary<string, object?> { [argumentName] = "Find the project plan" })]));
            }

            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "Found the project plan."));
        });
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton(new Microsoft365RetrievalSearch(retrievalClient))
            .BuildServiceProvider();
        IChatClient client = new ChatClientBuilder(_ => innerClient)
            .UseMicrosoft365Retrieval(TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling)
            .Build(services);
        AIAgent agent = client.AsAIAgent(
            new ChatClientAgentOptions { UseProvidedChatClientAsIs = true },
            services: services);

        await foreach (AgentResponseUpdate _ in agent.RunStreamingAsync(
            [new ChatMessage(ChatRole.User, "Find the project plan")],
            cancellationToken: TestContext.Current.CancellationToken))
        {
        }

        Assert.Equal(["model", "retrieval", "model"], events);
        Assert.Equal(2, innerClient.ResponseCallCount);
        Assert.Equal("Find the project plan", retrievalClient.LastQuery);
        Assert.Equal(TestContext.Current.CancellationToken, retrievalClient.LastCancellationToken);
        Assert.Contains(innerClient.LastMessages, message =>
            message.Contents.OfType<FunctionResultContent>().Any());
    }

    private static Microsoft365RetrievalHit CreateHit()
    {
        Microsoft365RetrievalExtract extract = (Microsoft365RetrievalExtract)typeof(Microsoft365RetrievalExtract)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single()
            .Invoke(["The project plan content.", null]);
        using JsonDocument document = JsonDocument.Parse("{\"title\":\"Project plan\"}");
        Dictionary<string, JsonElement> metadata = document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value);

        return (Microsoft365RetrievalHit)typeof(Microsoft365RetrievalHit)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single()
            .Invoke([
                "https://contoso.sharepoint.com/sites/projects/plan.docx",
                new[] { extract },
                null,
                metadata]);
    }

    private sealed class StubRetrievalClient(List<string> events, IReadOnlyList<Microsoft365RetrievalHit>? hits = null)
        : IMicrosoft365RetrievalClient
    {
        public string? LastQuery { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task<IReadOnlyList<Microsoft365RetrievalHit>> RetrieveAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            events.Add("retrieval");
            LastQuery = query;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(hits ?? (IReadOnlyList<Microsoft365RetrievalHit>)[]);
        }
    }
}