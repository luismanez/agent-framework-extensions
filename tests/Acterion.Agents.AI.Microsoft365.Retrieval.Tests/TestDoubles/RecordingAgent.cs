using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

internal sealed class RecordingAgent : AIAgent
{
	private readonly List<string>? eventSink;

	public RecordingAgent(List<string>? eventSink = null)
	{
		this.eventSink = eventSink;
	}

	public List<string> Events { get; } = [];

	public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

	protected override ValueTask<AgentSession> CreateSessionCoreAsync(CancellationToken cancellationToken) =>
		ValueTask.FromException<AgentSession>(
			new NotSupportedException("This test agent does not use sessions."));

	protected override ValueTask<AgentSession> DeserializeSessionCoreAsync(
		JsonElement element,
		JsonSerializerOptions? serializerOptions,
		CancellationToken cancellationToken) =>
		ValueTask.FromException<AgentSession>(
			new NotSupportedException("This test agent does not use sessions."));

	protected override Task<AgentResponse> RunCoreAsync(
		IEnumerable<ChatMessage> messages,
		AgentSession? session,
		AgentRunOptions? options,
		CancellationToken cancellationToken)
	{
		LastMessages = messages.ToArray();
		Events.Add("agent");
		eventSink?.Add("agent");
		return Task.FromResult(new AgentResponse());
	}

	protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
		IEnumerable<ChatMessage> messages,
		AgentSession? session,
		AgentRunOptions? options,
		[System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
	{
		LastMessages = messages.ToArray();
		Events.Add("agent");
		eventSink?.Add("agent");
		await Task.CompletedTask;
		yield return new AgentResponseUpdate();
	}

	protected override ValueTask<JsonElement> SerializeSessionCoreAsync(
		AgentSession session,
		JsonSerializerOptions? serializerOptions,
		CancellationToken cancellationToken) =>
		ValueTask.FromResult(JsonSerializer.SerializeToElement(new { }));
}