namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

internal sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
{
    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;
        return Task.FromException<HttpResponseMessage>(exception);
    }
}
