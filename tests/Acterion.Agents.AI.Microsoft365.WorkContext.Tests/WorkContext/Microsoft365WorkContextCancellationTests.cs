using System.Net;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class Microsoft365WorkContextCancellationTests
{
    [Theory]
    [InlineData(WorkContextErrorBehavior.BestEffort)]
    [InlineData(WorkContextErrorBehavior.FailFast)]
    public async Task GetSnapshotAsync_CancellationDuringTokenAcquisitionPropagatesWithoutHttp(
        WorkContextErrorBehavior errorBehavior)
    {
        using CancellationTokenSource source = new();
        CountingTokenProvider tokenProvider = new(source, cancelOnCall: true);
        CountingHandler handler = new();
        Microsoft365WorkContextClient client = CreateClient(tokenProvider, handler, errorBehavior: errorBehavior);

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetSnapshotAsync(source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(0, handler.CallCount);
    }

    [Theory]
    [InlineData(false, WorkContextErrorBehavior.BestEffort)]
    [InlineData(true, WorkContextErrorBehavior.BestEffort)]
    [InlineData(false, WorkContextErrorBehavior.FailFast)]
    [InlineData(true, WorkContextErrorBehavior.FailFast)]
    public async Task GetSnapshotAsync_CancellationDuringSendPropagatesWithoutRetry(
        bool batch,
        WorkContextErrorBehavior errorBehavior)
    {
        using CancellationTokenSource source = new();
        CountingTokenProvider tokenProvider = new(source);
        CountingHandler handler = new(source, cancelOnSend: true);
        Microsoft365WorkContextClient client = CreateClient(tokenProvider, handler, batch, errorBehavior);

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetSnapshotAsync(source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(1, handler.CallCount);
    }

    [Theory]
    [InlineData(false, WorkContextErrorBehavior.BestEffort)]
    [InlineData(true, WorkContextErrorBehavior.BestEffort)]
    [InlineData(false, WorkContextErrorBehavior.FailFast)]
    [InlineData(true, WorkContextErrorBehavior.FailFast)]
    public async Task GetSnapshotAsync_CancellationDuringResponseReadPropagatesWithoutConversion(
        bool batch,
        WorkContextErrorBehavior errorBehavior)
    {
        using CancellationTokenSource source = new();
        CountingTokenProvider tokenProvider = new(source);
        CountingHandler handler = new(source, cancelOnRead: true);
        Microsoft365WorkContextClient client = CreateClient(tokenProvider, handler, batch, errorBehavior);

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetSnapshotAsync(source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(1, handler.CallCount);
        Assert.Equal(1, handler.ReadCount);
    }

    [Fact]
    public async Task GetSnapshotAsync_PreCancelledCallDoesNotAcquireTokenOrSendHttp()
    {
        using CancellationTokenSource source = new();
        source.Cancel();
        CountingTokenProvider tokenProvider = new(source);
        CountingHandler handler = new();
        Microsoft365WorkContextClient client = CreateClient(tokenProvider, handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetSnapshotAsync(source.Token));

        Assert.Equal(0, tokenProvider.CallCount);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetSnapshotAsync_TokenProviderCancelsThenReturnsBlank_PropagatesCancellation()
    {
        using CancellationTokenSource source = new();
        CountingTokenProvider tokenProvider = new(source, returnBlankAfterCancel: true);
        CountingHandler handler = new();
        Microsoft365WorkContextClient client = CreateClient(tokenProvider, handler);

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetSnapshotAsync(source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(0, handler.CallCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetSnapshotAsync_HandlerCancelsThenReturnsSuccess_DoesNotComplete(bool batch)
    {
        using CancellationTokenSource source = new();
        CountingTokenProvider tokenProvider = new(source);
        string responseJson = batch
            ? """{ "responses": [ { "id": "profile", "status": 200, "body": {} }, { "id": "manager", "status": 404 } ] }"""
            : "{}";
        CountingHandler handler = new(source, cancelAndSucceed: true, responseJson: responseJson);
        Microsoft365WorkContextClient client = CreateClient(tokenProvider, handler, batch);

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetSnapshotAsync(source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(1, handler.CallCount);
    }

    private static Microsoft365WorkContextClient CreateClient(
        CountingTokenProvider tokenProvider,
        CountingHandler handler,
        bool batch = false,
        WorkContextErrorBehavior errorBehavior = WorkContextErrorBehavior.FailFast) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/") },
            tokenProvider,
            new Microsoft365WorkContextOptions
            {
                EnableManager = batch,
                ErrorBehavior = errorBehavior,
            },
            TimeProvider.System);

    private sealed class CountingTokenProvider(
        CancellationTokenSource source,
        bool cancelOnCall = false,
        bool returnBlankAfterCancel = false)
        : IMicrosoft365WorkContextTokenProvider
    {
        public int CallCount { get; private set; }

        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (cancelOnCall)
            {
                source.Cancel();
                return Task.FromCanceled<string>(source.Token);
            }

            if (returnBlankAfterCancel)
            {
                source.Cancel();
                return Task.FromResult(" ");
            }

            return Task.FromResult("delegated-token");
        }
    }

    private sealed class CountingHandler(
        CancellationTokenSource? source = null,
        bool cancelOnSend = false,
        bool cancelOnRead = false,
        bool cancelAndSucceed = false,
        string responseJson = "{}") : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        public int ReadCount => this.content?.ReadCount ?? 0;

        private CancelOnReadContent? content;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (cancelOnSend)
            {
                source!.Cancel();
                return Task.FromCanceled<HttpResponseMessage>(source.Token);
            }

            if (cancelAndSucceed)
            {
                source!.Cancel();
            }

            this.content = cancelOnRead ? new CancelOnReadContent(source!) : null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = this.content is null ? new StringContent(responseJson) : this.content,
            });
        }
    }

    private sealed class CancelOnReadContent(CancellationTokenSource source) : HttpContent
    {
        public int ReadCount { get; private set; }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            throw new NotSupportedException();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() => CancelRead();

        protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken) => CancelRead();

        private Task<Stream> CancelRead()
        {
            ReadCount++;
            source.Cancel();
            return Task.FromCanceled<Stream>(source.Token);
        }
    }
}
