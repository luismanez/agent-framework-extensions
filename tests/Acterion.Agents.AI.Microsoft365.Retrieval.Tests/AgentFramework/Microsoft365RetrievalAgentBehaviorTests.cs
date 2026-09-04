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
        RecordingAgent innerAgent = new(events);
        StubRetrievalClient retrievalClient = new(events, [CreateHit()]);
        using ServiceProvider services = new ServiceCollection()
            .AddSingleton(new Microsoft365RetrievalSearch(retrievalClient))
            .BuildServiceProvider();
        AIAgent agent = new AIAgentBuilder(innerAgent)
            .UseMicrosoft365Retrieval(TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke)
            .Build(services);

        await foreach (AgentResponseUpdate _ in agent.RunStreamingAsync(
            [new ChatMessage(ChatRole.User, "Find the project plan")],
            cancellationToken: TestContext.Current.CancellationToken))
        {
        }

        Assert.Equal(["retrieval", "agent"], events);
        Assert.Equal(["agent"], innerAgent.Events);
        ChatMessage context = Assert.Single(
            innerAgent.LastMessages,
            message => message.Role == ChatRole.User && message.Text != "Find the project plan");
        Assert.Contains("Project plan", context.Text);
        Assert.Contains("https://contoso.sharepoint.com/sites/projects/plan.docx", context.Text);
        Assert.Contains("The project plan content.", context.Text);
    }

    [Fact]
    public void OnDemandFunctionCalling_FailsDuringConfiguration()
    {
        NotSupportedException exception = Assert.Throws<NotSupportedException>(() =>
            new AIAgentBuilder(new RecordingAgent())
                .UseMicrosoft365Retrieval(TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling));

        Assert.Contains("OnDemandFunctionCalling", exception.Message, StringComparison.Ordinal);
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
        public Task<IReadOnlyList<Microsoft365RetrievalHit>> RetrieveAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            events.Add("retrieval");
            return Task.FromResult(hits ?? (IReadOnlyList<Microsoft365RetrievalHit>)[]);
        }
    }
}