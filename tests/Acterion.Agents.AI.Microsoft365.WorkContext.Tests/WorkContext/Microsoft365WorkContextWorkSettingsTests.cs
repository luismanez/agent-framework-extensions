using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class Microsoft365WorkContextWorkSettingsTests
{
    private static readonly DateTimeOffset CapturedAtUtc =
        new(2026, 9, 25, 8, 30, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("Pacific Standard Time", "GMT Standard Time")]
    [InlineData("America/Los_Angeles", "Europe/London")]
    [InlineData("Contoso Custom Zone", "Fabrikam Working Zone")]
    public async Task GetSnapshotAsync_WhenAllChildrenSucceed_ReturnsOpaqueImmutableSettings(
        string mailboxTimeZone,
        string workingHoursTimeZone)
    {
        string responseJson = $$"""
            {
              "responses": [
                {
                  "id": "work-hours",
                  "status": 200,
                  "body": {
                    "daysOfWeek": ["monday", "wednesday", "friday"],
                    "startTime": "08:15:00.0000000",
                    "endTime": "17:45:00.0000000",
                    "timeZone": {
                      "name": {{JsonSerializer.Serialize(workingHoursTimeZone)}},
                      "bias": -120,
                      "unknown": "discarded"
                    },
                    "unknown": "discarded"
                  }
                },
                {
                  "id": "work-time-zone",
                  "status": 200,
                  "body": {{JsonSerializer.Serialize(mailboxTimeZone)}}
                },
                {
                  "id": "work-language",
                  "status": 200,
                  "body": {
                    "locale": "en-US",
                    "displayName": "English (United States)",
                    "emailAddress": "sensitive@example.com",
                    "unknown": "discarded"
                  }
                }
              ]
            }
            """;
        RecordingBatchHandler handler = new(responseJson);
        Microsoft365WorkContextClient client = CreateClient(handler);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.CallCount);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.UserProfile.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Manager.Status);
        Assert.Equal(WorkContextFacetStatus.Available, snapshot.WorkSettings.Status);
        Assert.Null(snapshot.WorkSettings.Failure);
        WorkContextWorkSettings settings = Assert.IsType<WorkContextWorkSettings>(snapshot.WorkSettings.Value);
        Assert.Equal(mailboxTimeZone, settings.TimeZone);
        WorkContextLocale language = Assert.IsType<WorkContextLocale>(settings.Language);
        Assert.Equal("en-US", language.Locale);
        Assert.Equal("English (United States)", language.DisplayName);
        WorkContextWorkingHours workingHours =
            Assert.IsType<WorkContextWorkingHours>(settings.WorkingHours);
        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday], workingHours.DaysOfWeek);
        Assert.Equal(new TimeOnly(8, 15), workingHours.StartTime);
        Assert.Equal(new TimeOnly(17, 45), workingHours.EndTime);
        Assert.Equal(workingHoursTimeZone, workingHours.TimeZone);
        Assert.True(Assert.IsAssignableFrom<ICollection<DayOfWeek>>(workingHours.DaysOfWeek).IsReadOnly);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Calendar.Status);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenAllChildrenAreAbsent_ReturnsUnavailable()
    {
        RecordingBatchHandler handler = new("""
            {
              "responses": [
                { "id": "work-hours", "status": 204 },
                { "id": "work-time-zone", "status": 204 },
                { "id": "work-language", "status": 204 }
              ]
            }
            """);
        Microsoft365WorkContextClient client = CreateClient(handler);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Unavailable, snapshot.WorkSettings.Status);
        Assert.Null(snapshot.WorkSettings.Value);
        Assert.Null(snapshot.WorkSettings.Failure);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenOneChildSucceedsAndOthersAreAbsent_ReturnsAvailablePartialSettings()
    {
        RecordingBatchHandler handler = new("""
            {
              "responses": [
                { "id": "work-language", "status": 204 },
                { "id": "work-time-zone", "status": 200, "body": "Europe/Madrid" },
                { "id": "work-hours", "status": 204 }
              ]
            }
            """);
        Microsoft365WorkContextClient client = CreateClient(handler);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Available, snapshot.WorkSettings.Status);
        Assert.Null(snapshot.WorkSettings.Failure);
        WorkContextWorkSettings settings = Assert.IsType<WorkContextWorkSettings>(snapshot.WorkSettings.Value);
        Assert.Equal("Europe/Madrid", settings.TimeZone);
        Assert.Null(settings.Language);
        Assert.Null(settings.WorkingHours);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetSnapshotAsync_WhenChildrenFail_SelectsFixedOrderAndPreservesSuccessfulSibling(
        bool reverseBatchOrder)
    {
        RecordingBatchHandler handler = new(PartialFailureResponse(reverseBatchOrder));
        Microsoft365WorkContextClient client = CreateClient(handler);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.WorkSettings.Status);
        WorkContextFacetFailure failure =
            Assert.IsType<WorkContextFacetFailure>(snapshot.WorkSettings.Failure);
        Assert.Equal(WorkContextFailureKind.Service, failure.Kind);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failure.StatusCode);
        Assert.Equal("time-zone-request-id", failure.RequestId);
        WorkContextWorkSettings settings = Assert.IsType<WorkContextWorkSettings>(snapshot.WorkSettings.Value);
        Assert.Null(settings.TimeZone);
        WorkContextLocale language = Assert.IsType<WorkContextLocale>(settings.Language);
        Assert.Equal("es-ES", language.Locale);
        Assert.Equal("Spanish (Spain)", language.DisplayName);
        Assert.Null(settings.WorkingHours);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenChildFailsWithoutAnySuccess_ReturnsNullPartialValue()
    {
        RecordingBatchHandler handler = new("""
            {
              "responses": [
                { "id": "work-hours", "status": 204 },
                {
                  "id": "work-language",
                  "status": 403,
                  "headers": { "request-id": "language-request-id" },
                  "body": { "error": { "message": "sensitive Graph error body" } }
                },
                { "id": "work-time-zone", "status": 204 }
              ]
            }
            """);
        Microsoft365WorkContextClient client = CreateClient(handler);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.WorkSettings.Status);
        Assert.Null(snapshot.WorkSettings.Value);
        WorkContextFacetFailure failure =
            Assert.IsType<WorkContextFacetFailure>(snapshot.WorkSettings.Failure);
        Assert.Equal(WorkContextFailureKind.Authorization, failure.Kind);
        Assert.Equal(HttpStatusCode.Forbidden, failure.StatusCode);
        Assert.Equal("language-request-id", failure.RequestId);
    }

    [Theory]
    [InlineData("funday", "08:00:00.0000000")]
    [InlineData("monday", "not-a-time")]
    public async Task GetSnapshotAsync_WhenSuccessfulChildIsMalformed_ReturnsInvalidResponseWithPartialValue(
        string dayOfWeek,
        string startTime)
    {
        RecordingBatchHandler handler = new($$"""
            {
              "responses": [
                {
                  "id": "work-hours",
                  "status": 200,
                  "body": {
                    "daysOfWeek": [{{JsonSerializer.Serialize(dayOfWeek)}}],
                    "startTime": {{JsonSerializer.Serialize(startTime)}},
                    "endTime": "17:00:00.0000000",
                    "timeZone": { "name": "Custom Zone" }
                  }
                },
                { "id": "work-language", "status": 204 },
                { "id": "work-time-zone", "status": 200, "body": "Europe/Madrid" }
              ]
            }
            """);
        Microsoft365WorkContextClient client = CreateClient(handler);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.WorkSettings.Status);
        WorkContextFacetFailure failure =
            Assert.IsType<WorkContextFacetFailure>(snapshot.WorkSettings.Failure);
        Assert.Equal(WorkContextFailureKind.InvalidResponse, failure.Kind);
        Assert.Equal(HttpStatusCode.OK, failure.StatusCode);
        Assert.Null(failure.RequestId);
        WorkContextWorkSettings settings = Assert.IsType<WorkContextWorkSettings>(snapshot.WorkSettings.Value);
        Assert.Equal("Europe/Madrid", settings.TimeZone);
        Assert.Null(settings.Language);
        Assert.Null(settings.WorkingHours);
    }

    private static string PartialFailureResponse(bool reverseBatchOrder)
    {
        string[] responses =
        [
            """
            {
              "id": "work-time-zone",
              "status": 503,
              "headers": { "request-id": "time-zone-request-id" },
              "body": { "error": { "message": "sensitive Graph error body" } }
            }
            """,
            """
            {
              "id": "work-language",
              "status": 200,
              "body": { "locale": "es-ES", "displayName": "Spanish (Spain)" }
            }
            """,
            """
            {
              "id": "work-hours",
              "status": 401,
              "headers": { "request-id": "working-hours-request-id" },
              "body": { "error": { "message": "sensitive Graph error body" } }
            }
            """,
        ];

        if (reverseBatchOrder)
        {
            Array.Reverse(responses);
        }

        return $$"""
            { "responses": [{{string.Join(',', responses)}}] }
            """;
    }

    private static Microsoft365WorkContextClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/") },
            new StaticTokenProvider(),
            new Microsoft365WorkContextOptions
            {
                EnableUserProfile = false,
                EnableManager = false,
                EnableWorkSettings = true,
                EnableCalendar = false,
            },
            new FixedTimeProvider(CapturedAtUtc));

    private sealed class RecordingBatchHandler(string responseJson) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class StaticTokenProvider : IMicrosoft365WorkContextTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("delegated-token");
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
