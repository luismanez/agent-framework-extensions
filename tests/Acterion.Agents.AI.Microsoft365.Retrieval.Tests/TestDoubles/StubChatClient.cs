using Microsoft.Extensions.AI;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

internal sealed class StubChatClient : IChatClient
{
    private readonly Func<int, ChatOptions?, ChatResponse>? _responseFactory;

    public StubChatClient(Func<int, ChatOptions?, ChatResponse>? responseFactory = null)
    {
        _responseFactory = responseFactory;
    }

    public ChatOptions? LastOptions { get; private set; }

    public IReadOnlyList<ChatMessage> LastMessages { get; private set; } = [];

    public int ResponseCallCount { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        LastMessages = messages.ToArray();
        return Task.FromResult(CreateResponse(options));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LastMessages = messages.ToArray();
        ChatResponse response = CreateResponse(options);

        foreach (ChatMessage message in response.Messages)
        {
            ChatResponseUpdate update = new(message.Role, message.Contents)
            {
                FinishReason = message.Contents.OfType<FunctionCallContent>().Any()
                    ? ChatFinishReason.ToolCalls
                    : ChatFinishReason.Stop,
            };

            yield return update;
        }
    }

    private ChatResponse CreateResponse(ChatOptions? options)
    {
        LastOptions = options;
        ResponseCallCount++;

        return _responseFactory?.Invoke(ResponseCallCount, options) ?? new ChatResponse();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}