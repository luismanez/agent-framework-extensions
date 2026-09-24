using System.Net;
using System.Text;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class Microsoft365WorkContextBatchRequestTests
{
    private static readonly DateTimeOffset CapturedAtUtc =
        new(2026, 9, 13, 12, 34, 56, TimeSpan.Zero);

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    public async Task GetSnapshotAsync_WhenMultipleOperationsSelected_SendsOneBatch(int enabledFacets)
    {
        RecordingTokenProvider tokenProvider = new("delegated-token");
        RecordingHttpMessageHandler handler = new();
        Microsoft365WorkContextClient client = CreateClient(enabledFacets, tokenProvider, handler);

        await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/$batch", handler.RequestUri?.AbsoluteUri);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("delegated-token", handler.AuthorizationParameter);
        Assert.Equal("application/json", handler.ContentType);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenAllFacetsEnabled_SerializesCanonicalSixOperationPayload()
    {
        RecordingTokenProvider tokenProvider = new("delegated-token");
        RecordingHttpMessageHandler handler = new();
        Microsoft365WorkContextClient client = CreateClient(15, tokenProvider, handler);

        await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            "{\"requests\":[{\"id\":\"profile\",\"method\":\"GET\",\"url\":\"/me?%24select=displayName%2CgivenName%2Csurname%2CjobTitle%2Cdepartment%2CofficeLocation%2CpreferredLanguage\"}," +
            "{\"id\":\"manager\",\"method\":\"GET\",\"url\":\"/me/manager?%24select=displayName%2CjobTitle%2Cdepartment%2CofficeLocation\"}," +
            "{\"id\":\"work-time-zone\",\"method\":\"GET\",\"url\":\"/me/mailboxSettings/timeZone\"}," +
            "{\"id\":\"work-language\",\"method\":\"GET\",\"url\":\"/me/mailboxSettings/language\"}," +
            "{\"id\":\"work-hours\",\"method\":\"GET\",\"url\":\"/me/mailboxSettings/workingHours\"}," +
            "{\"id\":\"calendar\",\"method\":\"GET\",\"url\":\"/me/calendar/calendarView?startDateTime=2026-09-13T12%3A34%3A56.0000000%2B00%3A00\\u0026endDateTime=2026-09-14T12%3A34%3A56.0000000%2B00%3A00\\u0026%24top=10\\u0026%24select=subject%2Cstart%2Cend%2CoriginalStartTimeZone%2CoriginalEndTimeZone%2Clocation%2Corganizer%2Cattendees%2CisAllDay%2CisCancelled%2Csensitivity%2CshowAs%2CresponseStatus\"}]}",
            handler.Body);
        Assert.DoesNotContain("dependsOn", handler.Body, StringComparison.Ordinal);
    }

    private static Microsoft365WorkContextClient CreateClient(
        int enabledFacets,
        RecordingTokenProvider tokenProvider,
        RecordingHttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/") },
            tokenProvider,
            new Microsoft365WorkContextOptions
            {
                EnableUserProfile = (enabledFacets & 1) != 0,
                EnableManager = (enabledFacets & 2) != 0,
                EnableWorkSettings = (enabledFacets & 4) != 0,
                EnableCalendar = (enabledFacets & 8) != 0,
            },
            new FixedTimeProvider(CapturedAtUtc));

    private sealed class RecordingTokenProvider(string accessToken) : IMicrosoft365WorkContextTokenProvider
    {
        public int CallCount { get; private set; }

        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(accessToken);
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public string? AuthorizationParameter { get; private set; }

        public string? AuthorizationScheme { get; private set; }

        public string? Body { get; private set; }

        public string? ContentType { get; private set; }

        public HttpMethod? Method { get; private set; }

        public int RequestCount { get; private set; }

        public Uri? RequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            Method = request.Method;
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"responses\":[]}", Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
