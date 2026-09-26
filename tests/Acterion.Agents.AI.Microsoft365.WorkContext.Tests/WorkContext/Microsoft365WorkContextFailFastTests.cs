using System.Net;
using System.Text;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class Microsoft365WorkContextFailFastTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetSnapshotAsync_SelectsFirstFailedOperationRegardlessOfBatchResponseOrder(bool reverse)
    {
        string[] responses =
        [
            """{ "id": "calendar", "status": 429, "headers": { "request-id": "00000000-0000-4000-8000-000000000001" } }""",
            """{ "id": "manager", "status": 404 }""",
            """{ "id": "profile", "status": 403, "headers": { "request-id": "00000000-0000-4000-8000-000000000008" }, "body": { "error": { "message": "secret-graph-message" } } }""",
        ];
        if (reverse)
        {
            Array.Reverse(responses);
        }

        RecordingHandler handler = new($$"""{ "responses": [{{string.Join(',', responses)}}] }""");
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableUserProfile = true, EnableManager = true, EnableCalendar = true, ErrorBehavior = WorkContextErrorBehavior.FailFast },
            handler);

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Equal(WorkContextFacet.UserProfile, exception.Facet);
        Assert.Equal(WorkContextFailureKind.Authorization, exception.Kind);
        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal("00000000-0000-4000-8000-000000000008", exception.RequestId);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain("secret-graph-message", exception.ToString());
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetSnapshotAsync_WorkSettingsUsesFixedChildFailureOrder()
    {
        RecordingHandler handler = new("""
            { "responses": [
              { "id": "work-hours", "status": 401, "headers": { "request-id": "00000000-0000-4000-8000-000000000004" } },
              { "id": "work-language", "status": 429, "headers": { "request-id": "00000000-0000-4000-8000-000000000005" } },
              { "id": "work-time-zone", "status": 503, "headers": { "request-id": "00000000-0000-4000-8000-000000000010" } }
            ] }
            """);
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableUserProfile = false, EnableWorkSettings = true, ErrorBehavior = WorkContextErrorBehavior.FailFast },
            handler);

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Equal(WorkContextFacet.WorkSettings, exception.Facet);
        Assert.Equal(WorkContextFailureKind.Service, exception.Kind);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
        Assert.Equal("00000000-0000-4000-8000-000000000010", exception.RequestId);
    }

    [Fact]
    public async Task GetSnapshotAsync_DocumentedManagerAbsenceDoesNotThrow()
    {
        RecordingHandler handler = new("""{ "responses": [ { "id": "manager", "status": 404 }, { "id": "profile", "status": 200, "body": { "displayName": "Avery" } } ] }""");
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableUserProfile = true, EnableManager = true, ErrorBehavior = WorkContextErrorBehavior.FailFast },
            handler);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Available, snapshot.UserProfile.Status);
        Assert.Equal(WorkContextFacetStatus.Unavailable, snapshot.Manager.Status);
        Assert.Null(snapshot.Manager.Failure);
    }

    [Fact]
    public async Task GetSnapshotAsync_AllAbsentWorkSettingsReturnUnavailable()
    {
        RecordingHandler handler = new("""
            { "responses": [
              { "id": "work-hours", "status": 204 },
              { "id": "work-language", "status": 204 },
              { "id": "work-time-zone", "status": 204 }
            ] }
            """);
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableUserProfile = false, EnableWorkSettings = true, ErrorBehavior = WorkContextErrorBehavior.FailFast }, handler);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Unavailable, snapshot.WorkSettings.Status);
        Assert.Null(snapshot.WorkSettings.Failure);
    }

    [Fact]
    public async Task GetSnapshotAsync_DirectGraphFailureNamesOwningFacet()
    {
        RecordingHandler handler = new("""{ "error": { "message": "sensitive" } }""", HttpStatusCode.Unauthorized);
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { ErrorBehavior = WorkContextErrorBehavior.FailFast }, handler);

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Equal(WorkContextFacet.UserProfile, exception.Facet);
        Assert.Equal(WorkContextFailureKind.Authentication, exception.Kind);
        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain("sensitive", exception.ToString());
    }

    [Fact]
    public async Task GetSnapshotAsync_DirectMalformedProfileThrowsSanitizedInvalidResponse()
    {
        RecordingHandler handler = new("""{ "displayName": 123, "mail": "address-canary@example.invalid" }""");
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { ErrorBehavior = WorkContextErrorBehavior.FailFast }, handler);

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Equal(WorkContextFacet.UserProfile, exception.Facet);
        Assert.Equal(WorkContextFailureKind.InvalidResponse, exception.Kind);
        Assert.Equal(HttpStatusCode.OK, exception.StatusCode);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain("address-canary", exception.ToString());
    }

    [Theory]
    [InlineData(WorkContextErrorBehavior.BestEffort)]
    [InlineData(WorkContextErrorBehavior.FailFast)]
    public async Task GetSnapshotAsync_DirectUnreadableContentIsSanitized(
        WorkContextErrorBehavior errorBehavior)
    {
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { ErrorBehavior = errorBehavior },
            new RecordingHandler("{}", customContent: new ThrowingReadContent()));

        if (errorBehavior == WorkContextErrorBehavior.BestEffort)
        {
            WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);
            Assert.Equal(WorkContextFacetStatus.Failed, snapshot.UserProfile.Status);
            Assert.Equal(WorkContextFailureKind.InvalidResponse, snapshot.UserProfile.Failure?.Kind);
        }
        else
        {
            Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
                () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));
            Assert.Equal(WorkContextFacet.UserProfile, exception.Facet);
            Assert.Equal(WorkContextFailureKind.InvalidResponse, exception.Kind);
            Assert.Null(exception.InnerException);
            Assert.DoesNotContain("secret-content-detail", exception.ToString());
        }
    }

    [Theory]
    [InlineData(false, WorkContextErrorBehavior.BestEffort)]
    [InlineData(true, WorkContextErrorBehavior.BestEffort)]
    [InlineData(false, WorkContextErrorBehavior.FailFast)]
    [InlineData(true, WorkContextErrorBehavior.FailFast)]
    public async Task GetSnapshotAsync_StreamIoFailureIsSanitizedAsTransport(
        bool batch,
        WorkContextErrorBehavior errorBehavior)
    {
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions
            {
                EnableManager = batch,
                ErrorBehavior = errorBehavior,
            },
            new RecordingHandler("{}", customContent: new ThrowingReadContent(
                new IOException("secret-stream-detail"))));

        if (errorBehavior == WorkContextErrorBehavior.BestEffort)
        {
            WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);
            Assert.Equal(WorkContextFailureKind.Transport, snapshot.UserProfile.Failure?.Kind);
            Assert.Equal(batch ? WorkContextFacetStatus.Failed : WorkContextFacetStatus.Disabled,
                snapshot.Manager.Status);
        }
        else
        {
            Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
                () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));
            Assert.Equal(batch ? null : WorkContextFacet.UserProfile, exception.Facet);
            Assert.Equal(WorkContextFailureKind.Transport, exception.Kind);
            Assert.Null(exception.InnerException);
            Assert.DoesNotContain("secret-stream-detail", exception.ToString());
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_MalformedCalendarEntryThrowsSanitizedInvalidResponse()
    {
        RecordingHandler handler = new("""{ "value": [{ "subject": "private-canary", "sensitivity": "unknown" }] }""");
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableUserProfile = false, EnableCalendar = true, ErrorBehavior = WorkContextErrorBehavior.FailFast }, handler);

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Equal(WorkContextFacet.Calendar, exception.Facet);
        Assert.Equal(WorkContextFailureKind.InvalidResponse, exception.Kind);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain("private-canary", exception.ToString());
    }

    [Fact]
    public async Task GetSnapshotAsync_OuterBatchFailureHasNoOwningFacet()
    {
        RecordingHandler handler = new("""{ "error": { "message": "secret-graph-message" } }""", HttpStatusCode.TooManyRequests);
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableManager = true, ErrorBehavior = WorkContextErrorBehavior.FailFast }, handler);

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Null(exception.Facet);
        Assert.Equal(WorkContextFailureKind.Throttled, exception.Kind);
        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain("secret-graph-message", exception.ToString());
    }

    [Fact]
    public async Task GetSnapshotAsync_MalformedOuterBatchThrowsSanitizedInvalidResponse()
    {
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableManager = true, ErrorBehavior = WorkContextErrorBehavior.FailFast },
            new RecordingHandler("{ \"responses\": ["));

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Null(exception.Facet);
        Assert.Equal(WorkContextFailureKind.InvalidResponse, exception.Kind);
        Assert.Equal(HttpStatusCode.OK, exception.StatusCode);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public async Task GetSnapshotAsync_NullBatchSubresponseIsSanitizedGlobalInvalidResponse()
    {
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableManager = true, ErrorBehavior = WorkContextErrorBehavior.FailFast },
            new RecordingHandler("""{ "responses": [null, { "id": "profile", "status": 200, "body": {} }] }"""));

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Null(exception.Facet);
        Assert.Equal(WorkContextFailureKind.InvalidResponse, exception.Kind);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public async Task GetSnapshotAsync_TokenProviderFailureThrowsSanitizedGlobalExceptionWithoutHttp()
    {
        RecordingHandler handler = new("{}");
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { ErrorBehavior = WorkContextErrorBehavior.FailFast },
            handler,
            new ThrowingTokenProvider(new InvalidOperationException("secret-token-detail")));

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Null(exception.Facet);
        Assert.Equal(WorkContextFailureKind.TokenAcquisition, exception.Kind);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain("secret-token-detail", exception.ToString());
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetSnapshotAsync_TransportFailureThrowsSanitizedGlobalException()
    {
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { ErrorBehavior = WorkContextErrorBehavior.FailFast },
            new ThrowingHttpHandler(new HttpRequestException("secret-transport-detail")));

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Null(exception.Facet);
        Assert.Equal(WorkContextFailureKind.Transport, exception.Kind);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain("secret-transport-detail", exception.ToString());
    }

    [Theory]
    [InlineData(false, WorkContextErrorBehavior.BestEffort)]
    [InlineData(false, WorkContextErrorBehavior.FailFast)]
    [InlineData(true, WorkContextErrorBehavior.BestEffort)]
    [InlineData(true, WorkContextErrorBehavior.FailFast)]
    public async Task GetSnapshotAsync_HttpTimeoutWithoutCallerCancellationIsTransportFailure(
        bool batch,
        WorkContextErrorBehavior errorBehavior)
    {
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableManager = batch, ErrorBehavior = errorBehavior },
            new ThrowingHttpHandler(new OperationCanceledException(
                "secret-timeout-detail",
                new TimeoutException("secret-inner-timeout-detail"))));

        if (errorBehavior == WorkContextErrorBehavior.BestEffort)
        {
            WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);
            Assert.Equal(WorkContextFailureKind.Transport, snapshot.UserProfile.Failure?.Kind);
            Assert.Equal(batch ? WorkContextFacetStatus.Failed : WorkContextFacetStatus.Disabled, snapshot.Manager.Status);
        }
        else
        {
            Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
                () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));
            Assert.Null(exception.Facet);
            Assert.Equal(WorkContextFailureKind.Transport, exception.Kind);
            Assert.Null(exception.InnerException);
            Assert.DoesNotContain("secret-timeout-detail", exception.ToString());
        }
    }

    [Theory]
    [InlineData(WorkContextErrorBehavior.BestEffort)]
    [InlineData(WorkContextErrorBehavior.FailFast)]
    public async Task GetSnapshotAsync_SendIoFailureIsSanitizedAsGlobalTransport(
        WorkContextErrorBehavior errorBehavior)
    {
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { ErrorBehavior = errorBehavior },
            new ThrowingHttpHandler(new IOException("secret-send-detail")));

        if (errorBehavior == WorkContextErrorBehavior.BestEffort)
        {
            WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);
            Assert.Equal(WorkContextFailureKind.Transport, snapshot.UserProfile.Failure?.Kind);
        }
        else
        {
            Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
                () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));
            Assert.Null(exception.Facet);
            Assert.Equal(WorkContextFailureKind.Transport, exception.Kind);
            Assert.Null(exception.InnerException);
            Assert.DoesNotContain("secret-send-detail", exception.ToString());
        }
    }

    internal static Microsoft365WorkContextClient CreateClient(
        Microsoft365WorkContextOptions options,
        HttpMessageHandler handler,
        IMicrosoft365WorkContextTokenProvider? tokenProvider = null) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/") },
            tokenProvider ?? new StaticTokenProvider(),
            options,
            new FixedTimeProvider());

    internal sealed class RecordingHandler(
        string responseJson,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        HttpContent? customContent = null) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = customContent ?? new StringContent(responseJson, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class ThrowingReadContent(Exception? exception = null) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            throw new NotSupportedException();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override Task<Stream> CreateContentReadStreamAsync() =>
            Task.FromException<Stream>(exception ?? new InvalidDataException("secret-content-detail"));

        protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken) =>
            CreateContentReadStreamAsync();
    }

    private sealed class ThrowingTokenProvider(Exception exception) : IMicrosoft365WorkContextTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<string>(exception);
    }

    private sealed class ThrowingHttpHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(exception);
    }

    private sealed class StaticTokenProvider : IMicrosoft365WorkContextTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("delegated-token");
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
    }
}
