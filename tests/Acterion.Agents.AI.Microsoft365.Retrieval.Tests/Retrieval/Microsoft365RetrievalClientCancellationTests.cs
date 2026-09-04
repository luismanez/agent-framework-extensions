using System.Net;
using System.Text;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class Microsoft365RetrievalClientCancellationTests
{
    [Fact]
    public async Task RetrieveAsync_PropagatesTokenProviderFailureUnchanged()
    {
        InvalidOperationException tokenException = new("token provider failed");
        using HttpResponseMessage response = new(HttpStatusCode.OK);
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler);
        Microsoft365RetrievalClient client = new(
            httpClient,
            new FailingTokenProvider(tokenException),
            new Microsoft365RetrievalOptions());

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.RetrieveAsync("engineering plan", TestContext.Current.CancellationToken));

        Assert.Same(tokenException, exception);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task RetrieveAsync_PropagatesTokenAcquisitionCancellationUnchanged()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        using HttpResponseMessage response = new(HttpStatusCode.OK);
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler);
        Microsoft365RetrievalClient client = new(
            httpClient,
            new CancelingTokenProvider(),
            new Microsoft365RetrievalOptions());

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.RetrieveAsync("engineering plan", cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task RetrieveAsync_PropagatesSendCancellationUnchanged()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        using ThrowingHttpMessageHandler handler = new(new OperationCanceledException(cancellation.Token));
        using HttpClient httpClient = new(handler);
        Microsoft365RetrievalClient client = new(
            httpClient,
            new StubTokenProvider(),
            new Microsoft365RetrievalOptions());

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.RetrieveAsync("engineering plan", cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task RetrieveAsync_PropagatesResponseReadCancellationUnchanged()
    {
        using CancellationTokenSource cancellation = new();
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"retrievalHits\":[]}", Encoding.UTF8, "application/json"),
        };
        using CancelingHttpMessageHandler handler = new(response, cancellation);
        using HttpClient httpClient = new(handler);
        Microsoft365RetrievalClient client = new(
            httpClient,
            new StubTokenProvider(),
            new Microsoft365RetrievalOptions());

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.RetrieveAsync("engineering plan", cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(1, handler.RequestCount);
    }

    private sealed class FailingTokenProvider(Exception exception) : IMicrosoft365RetrievalTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<string>(exception);
    }

    private sealed class CancelingTokenProvider : IMicrosoft365RetrievalTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromCanceled<string>(cancellationToken);
    }
}
