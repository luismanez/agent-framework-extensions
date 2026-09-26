using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class Microsoft365WorkContextLoggingTests
{
    [Fact]
    public async Task GetSnapshotAsync_DirectSuccessEmitsStructuredStartAndCompletion()
    {
        RecordingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions(),
            new ResponseHandler("""{ "displayName": "private-profile-name" }"""),
            logger);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Available, snapshot.UserProfile.Status);
        Assert.Equal([2000, 2001], logger.Entries.Select(entry => entry.EventId.Id));
        Assert.Equal(1, logger.Entries[0].Field<int>("OperationCount"));
        Assert.False(logger.Entries[0].Field<bool>("IsBatch"));
        Assert.Equal(WorkContextFacetStatus.Available, logger.Entries[1].Field<WorkContextFacetStatus>("UserProfileStatus"));
        Assert.Equal(WorkContextFacetStatus.Disabled, logger.Entries[1].Field<WorkContextFacetStatus>("ManagerStatus"));
        Assert.True(logger.Entries[1].Field<long>("ElapsedMilliseconds") >= 0);
        Assert.Equal(0, logger.Entries[1].Field<int>("EventCount"));
        Assert.False(logger.Entries[1].Field<bool>("AttendeesTruncated"));
        Assert.All(logger.Entries, entry => Assert.Null(entry.Exception));
        Assert.DoesNotContain("private-profile-name", logger.RenderedText);
    }

    [Fact]
    public async Task GetSnapshotAsync_BestEffortBatchFailureReportsFacetStatusAndSafeFailureFields()
    {
        RecordingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableManager = true },
            new ResponseHandler("""
                { "responses": [
                  { "id": "manager", "status": 403, "headers": { "request-id": "00000000-0000-4000-8000-000000000014" }, "body": { "error": { "message": "private-graph-message" } } },
                  { "id": "profile", "status": 200, "body": { "displayName": "private-profile-name" } }
                ] }
                """),
            logger);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.Manager.Status);
        Assert.Equal([2000, 2002, 2001], logger.Entries.Select(entry => entry.EventId.Id));
        Assert.Equal(2, logger.Entries[0].Field<int>("OperationCount"));
        Assert.True(logger.Entries[0].Field<bool>("IsBatch"));
        Assert.Equal(WorkContextFacetStatus.Available, logger.Entries[2].Field<WorkContextFacetStatus>("UserProfileStatus"));
        Assert.Equal(WorkContextFacetStatus.Failed, logger.Entries[2].Field<WorkContextFacetStatus>("ManagerStatus"));
        Assert.Equal(WorkContextFacet.Manager, logger.Entries[1].Field<WorkContextFacet?>("Facet"));
        Assert.Equal(WorkContextFailureKind.Authorization, logger.Entries[1].Field<WorkContextFailureKind?>("FailureKind"));
        Assert.Equal(403, logger.Entries[1].Field<int?>("StatusCode"));
        Assert.Equal("00000000-0000-4000-8000-000000000014", logger.Entries[1].Field<string>("RequestId"));
        Assert.DoesNotContain("private-graph-message", logger.RenderedText);
        Assert.DoesNotContain("private-profile-name", logger.RenderedText);
    }

    [Fact]
    public async Task GetSnapshotAsync_BestEffortDirectFailureEmitsFailureAndCompletion()
    {
        RecordingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions(),
            new ResponseHandler("{}", HttpStatusCode.Forbidden), logger);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.UserProfile.Status);
        Assert.Equal([2000, 2002, 2001], logger.Entries.Select(entry => entry.EventId.Id));
        Assert.Equal(WorkContextFacet.UserProfile, logger.Entries[1].Field<WorkContextFacet?>("Facet"));
        Assert.Equal(WorkContextFailureKind.Authorization, logger.Entries[1].Field<WorkContextFailureKind>("FailureKind"));
    }

    [Fact]
    public async Task GetSnapshotAsync_BestEffortBatchLogsEachFailedFacet()
    {
        RecordingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableCalendar = true },
            new ResponseHandler("""
                { "responses": [
                  { "id": "calendar", "status": 429, "headers": { "request-id": "00000000-0000-4000-8000-000000000018" } },
                  { "id": "profile", "status": 403, "headers": { "request-id": "00000000-0000-4000-8000-000000000017" } }
                ] }
                """), logger);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.UserProfile.Status);
        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.Calendar.Status);
        Assert.Equal([2000, 2002, 2002, 2001], logger.Entries.Select(entry => entry.EventId.Id));
        Assert.Equal(WorkContextFacet.UserProfile, logger.Entries[1].Field<WorkContextFacet?>("Facet"));
        Assert.Equal(403, logger.Entries[1].Field<int?>("StatusCode"));
        Assert.Equal("00000000-0000-4000-8000-000000000017", logger.Entries[1].Field<string>("RequestId"));
        Assert.Equal(WorkContextFacet.Calendar, logger.Entries[2].Field<WorkContextFacet?>("Facet"));
        Assert.Equal(429, logger.Entries[2].Field<int?>("StatusCode"));
        Assert.Equal("00000000-0000-4000-8000-000000000018", logger.Entries[2].Field<string>("RequestId"));
        Assert.Equal(WorkContextFacetStatus.Failed, logger.Entries[3].Field<WorkContextFacetStatus>("CalendarStatus"));
    }

    [Fact]
    public async Task GetSnapshotAsync_GlobalTokenFailureLogsAffectedFacetsWithoutProviderDetail()
    {
        RecordingLogger logger = new();
        ResponseHandler handler = new("{}");
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableManager = true },
            handler, logger,
            new FailingTokenProvider());

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.UserProfile.Status);
        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.Manager.Status);
        Assert.Equal(0, handler.CallCount);
        Assert.Equal([2000, 2002, 2002, 2001], logger.Entries.Select(entry => entry.EventId.Id));
        Assert.All(logger.Entries.Where(entry => entry.EventId.Id == 2002), entry =>
            Assert.Equal(WorkContextFailureKind.TokenAcquisition, entry.Field<WorkContextFailureKind>("FailureKind")));
        Assert.DoesNotContain("private-provider-detail", logger.RenderedText);
    }

    [Theory]
    [InlineData(WorkContextErrorBehavior.BestEffort)]
    [InlineData(WorkContextErrorBehavior.FailFast)]
    public async Task GetSnapshotAsync_MappingFailureRetainsSafeResponseRequestId(
        WorkContextErrorBehavior errorBehavior)
    {
        const string requestId = "00000000-0000-4000-8000-000000000019";
        RecordingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { ErrorBehavior = errorBehavior },
            new ResponseHandler("""{ "displayName": [], "mail": "private-address@example.invalid" }""",
                requestId: requestId),
            logger);

        if (errorBehavior == WorkContextErrorBehavior.BestEffort)
        {
            WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);
            Assert.Equal(requestId, snapshot.UserProfile.Failure?.RequestId);
        }
        else
        {
            Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
                () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));
            Assert.Equal(requestId, exception.RequestId);
            Assert.DoesNotContain("private-address", exception.ToString());
        }

        Assert.Equal(requestId, logger.Entries[1].Field<string>("RequestId"));
        Assert.Equal(200, logger.Entries[1].Field<int?>("StatusCode"));
        Assert.DoesNotContain("private-address", logger.RenderedText);
    }

    [Fact]
    public async Task GetSnapshotAsync_FailFastFailureEmitsStartAndSanitizedFailure()
    {
        RecordingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { ErrorBehavior = WorkContextErrorBehavior.FailFast },
            new ResponseHandler("""{ "error": { "message": "private-graph-message" } }""", HttpStatusCode.Forbidden, "00000000-0000-4000-8000-000000000015"),
            logger);

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Equal(WorkContextFailureKind.Authorization, exception.Kind);
        Assert.Equal([2000, 2002], logger.Entries.Select(entry => entry.EventId.Id));
        Assert.Equal(LogLevel.Warning, logger.Entries[1].Level);
        Assert.Equal(WorkContextFacet.UserProfile, logger.Entries[1].Field<WorkContextFacet?>("Facet"));
        Assert.Equal(WorkContextFailureKind.Authorization, logger.Entries[1].Field<WorkContextFailureKind>("FailureKind"));
        Assert.Equal(403, logger.Entries[1].Field<int?>("StatusCode"));
        Assert.Equal("00000000-0000-4000-8000-000000000015", logger.Entries[1].Field<string>("RequestId"));
        Assert.Null(logger.Entries[1].Exception);
        Assert.DoesNotContain("private-graph-message", logger.RenderedText);
    }

    [Fact]
    public async Task GetSnapshotAsync_LogsCalendarCountsWithoutPersonalValues()
    {
        string attendees = string.Join(',', Enumerable.Range(1, 12).Select(index =>
            $$"""{ "emailAddress": { "name": "private-attendee-{{index}}", "address": "private-address-{{index}}@example.invalid" } }"""));
        string response = $$"""
            { "responses": [
              { "id": "profile", "status": 200, "body": { "displayName": "private-user-name", "department": "private-department" } },
              { "id": "manager", "status": 200, "body": { "displayName": "private-manager-name" } },
              { "id": "calendar", "status": 200, "body": { "value": [{
                "subject": "private-event-title", "sensitivity": "normal",
                "body": { "content": "private-description-canary" },
                "webLink": "https://example.invalid/private-link",
                "start": { "dateTime": "2026-09-26T09:00:00", "timeZone": "UTC" },
                "end": { "dateTime": "2026-09-26T10:00:00", "timeZone": "UTC" },
                "location": { "displayName": "private-location" },
                "organizer": { "emailAddress": { "name": "private-organizer" } },
                "attendees": [{{attendees}}]
              }] } }
            ] }
            """;
        RecordingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableManager = true, EnableCalendar = true },
            new ResponseHandler(response), logger);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, snapshot.Calendar.Value?.Count);
        Assert.Equal(1, logger.Entries[1].Field<int>("EventCount"));
        Assert.True(logger.Entries[1].Field<bool>("AttendeesTruncated"));
        foreach (string canary in new[]
        {
            "private-delegated-token", "private-scope-canary", "private-user-name", "private-department", "private-manager-name",
            "private-event-title", "private-location", "private-organizer", "private-attendee-1",
            "private-address-1", "private-description-canary", "private-link", "example.invalid",
            "graph.microsoft.com", "calendarView", "responses",
        })
        {
            Assert.DoesNotContain(canary, logger.RenderedText, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData(WorkContextErrorBehavior.BestEffort)]
    [InlineData(WorkContextErrorBehavior.FailFast)]
    public async Task GetSnapshotAsync_CancellationEmitsNoCompletionOrFailure(WorkContextErrorBehavior errorBehavior)
    {
        using CancellationTokenSource cancellation = new();
        RecordingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { ErrorBehavior = errorBehavior },
            new ResponseHandler("{}"), logger,
            new CancelingTokenProvider(cancellation));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetSnapshotAsync(cancellation.Token));

        Assert.Equal([2000], logger.Entries.Select(entry => entry.EventId.Id));
    }

    [Fact]
    public async Task GetSnapshotAsync_CancellationDuringFailureLoggingPreventsCompletion()
    {
        using CancellationTokenSource cancellation = new();
        RecordingLogger logger = new(eventId =>
        {
            if (eventId.Id == 2002)
            {
                cancellation.Cancel();
            }
        });
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions(),
            new ResponseHandler("{}", HttpStatusCode.Forbidden), logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetSnapshotAsync(cancellation.Token));

        Assert.Equal([2000, 2002], logger.Entries.Select(entry => entry.EventId.Id));
    }

    [Fact]
    public async Task GetSnapshotAsync_ThrowingLoggerCannotChangeSuccessfulSnapshot()
    {
        ThrowingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions(),
            new ResponseHandler("""{ "displayName": "private-user-name" }"""),
            logger);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Available, snapshot.UserProfile.Status);
        Assert.True(logger.CallCount > 0);
    }

    [Fact]
    public async Task GetSnapshotAsync_ThrowingLoggerCannotChangeBestEffortFailure()
    {
        ThrowingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions(),
            new ResponseHandler("{}", HttpStatusCode.Forbidden),
            logger);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.UserProfile.Status);
        Assert.Equal(WorkContextFailureKind.Authorization, snapshot.UserProfile.Failure?.Kind);
        Assert.True(logger.CallCount > 0);
    }

    [Fact]
    public async Task GetSnapshotAsync_ThrowingLoggerCannotChangeFailFastException()
    {
        ThrowingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { ErrorBehavior = WorkContextErrorBehavior.FailFast },
            new ResponseHandler("{}", HttpStatusCode.Forbidden),
            logger);

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Equal(WorkContextFailureKind.Authorization, exception.Kind);
        Assert.Equal(WorkContextFacet.UserProfile, exception.Facet);
        Assert.True(logger.CallCount > 0);
    }

    [Fact]
    public async Task GetSnapshotAsync_ThrowingLoggerCannotChangeCancellation()
    {
        using CancellationTokenSource cancellation = new();
        ThrowingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions(),
            new ResponseHandler("{}"),
            logger,
            new CancelingTokenProvider(cancellation));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetSnapshotAsync(cancellation.Token));
        Assert.True(logger.CallCount > 0);
    }

    [Fact]
    public async Task GetSnapshotAsync_RejectsPersonalDirectRequestIdFromLogAndException()
    {
        RecordingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { ErrorBehavior = WorkContextErrorBehavior.FailFast },
            new ResponseHandler("{}", HttpStatusCode.Forbidden, "private-user-name"), logger);

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => client.GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Null(exception.RequestId);
        Assert.Null(logger.Entries[1].Field<string?>("RequestId"));
        Assert.DoesNotContain("private-user-name", exception.ToString());
        Assert.DoesNotContain("private-user-name", logger.RenderedText);
    }

    [Fact]
    public async Task GetSnapshotAsync_RejectsRawUrlBatchRequestIdFromLogAndFacetFailure()
    {
        RecordingLogger logger = new();
        Microsoft365WorkContextClient client = CreateClient(
            new Microsoft365WorkContextOptions { EnableManager = true },
            new ResponseHandler("""
                { "responses": [
                  { "id": "profile", "status": 200, "body": {} },
                  { "id": "manager", "status": 403, "headers": { "request-id": "https://example.invalid/private-url" } }
                ] }
                """), logger);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Null(snapshot.Manager.Failure?.RequestId);
        Assert.Null(logger.Entries[1].Field<string?>("RequestId"));
        Assert.DoesNotContain("private-url", logger.RenderedText);
    }

    private static Microsoft365WorkContextClient CreateClient(
        Microsoft365WorkContextOptions options,
        HttpMessageHandler handler,
        ILogger<Microsoft365WorkContextClient> logger,
        IMicrosoft365WorkContextTokenProvider? tokenProvider = null) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/") },
            tokenProvider ?? new StaticTokenProvider(),
            options,
            TimeProvider.System,
            logger);

    private sealed class StaticTokenProvider : IMicrosoft365WorkContextTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("private-delegated-token-private-scope-canary");
    }

    private sealed class CancelingTokenProvider(CancellationTokenSource source) : IMicrosoft365WorkContextTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            source.Cancel();
            return Task.FromCanceled<string>(source.Token);
        }
    }

    private sealed class FailingTokenProvider : IMicrosoft365WorkContextTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<string>(new InvalidOperationException("private-provider-detail"));
    }

    private sealed class ThrowingLogger : ILogger<Microsoft365WorkContextClient>
    {
        public int CallCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            throw new InvalidOperationException("private-logger-detail");

        public bool IsEnabled(LogLevel logLevel) =>
            throw new InvalidOperationException("private-logger-detail");

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            this.CallCount++;
            throw new InvalidOperationException("private-logger-detail");
        }
    }

    private sealed class ResponseHandler(string json, HttpStatusCode statusCode = HttpStatusCode.OK, string? requestId = null)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.CallCount++;
            HttpResponseMessage response = new(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            if (requestId is not null)
            {
                response.Headers.TryAddWithoutValidation("request-id", requestId);
            }

            return Task.FromResult(response);
        }
    }

    private sealed class RecordingLogger(Action<EventId>? onLog = null) : ILogger<Microsoft365WorkContextClient>
    {
        public List<LogEntry> Entries { get; } = [];

        public string RenderedText => string.Join("\n", this.Entries.Select(entry =>
            entry.Message + " " + string.Join(" ", entry.Fields.Select(item => $"{item.Key}={item.Value}"))));

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            IReadOnlyDictionary<string, object?> fields = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
                : new Dictionary<string, object?>();
            this.Entries.Add(new LogEntry(logLevel, eventId, fields, formatter(state, exception), exception));
            onLog?.Invoke(eventId);
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        EventId EventId,
        IReadOnlyDictionary<string, object?> Fields,
        string Message,
        Exception? Exception)
    {
        public T Field<T>(string name) => (T)this.Fields[name]!;
    }
}
