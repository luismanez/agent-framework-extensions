namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

internal sealed class CancelingHttpMessageHandler(
    HttpResponseMessage response,
    CancellationTokenSource cancellationSource) : HttpMessageHandler
{
    public int RequestCount { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;
        cancellationSource.Cancel();
        return Task.FromResult(response);
    }
}
