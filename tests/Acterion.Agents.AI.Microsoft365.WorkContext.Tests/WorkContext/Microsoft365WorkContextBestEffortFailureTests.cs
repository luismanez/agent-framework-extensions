using System.Net;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class Microsoft365WorkContextBestEffortFailureTests
{
    private static readonly DateTimeOffset CapturedAtUtc =
        new(2026, 9, 24, 12, 34, 56, TimeSpan.Zero);

    [Fact]
    public async Task GetSnapshotAsync_WhenTokenProviderFails_ReturnsFailedEnabledFacets()
    {
        CountingHttpMessageHandler handler = new();
        ThrowingTokenProvider tokenProvider =
            new(new InvalidOperationException("sensitive token-provider detail"));
        Microsoft365WorkContextClient client = CreateClient(
            tokenProvider,
            handler,
            new Microsoft365WorkContextOptions
            {
                EnableUserProfile = true,
                EnableManager = true,
                EnableWorkSettings = true,
                EnableCalendar = true,
            });

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(CapturedAtUtc, snapshot.CapturedAtUtc);
        AssertFailed(snapshot.UserProfile, WorkContextFailureKind.TokenAcquisition);
        AssertFailed(snapshot.Manager, WorkContextFailureKind.TokenAcquisition);
        AssertFailed(snapshot.WorkSettings, WorkContextFailureKind.TokenAcquisition);
        AssertFailed(snapshot.Calendar, WorkContextFailureKind.TokenAcquisition);
        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenOnlyCalendarEnabledAndTokenProviderFails_ReturnsCalendarFailure()
    {
        CountingHttpMessageHandler handler = new();
        ThrowingTokenProvider tokenProvider =
            new(new InvalidOperationException("sensitive token-provider detail"));
        Microsoft365WorkContextClient client = CreateClient(
            tokenProvider,
            handler,
            new Microsoft365WorkContextOptions
            {
                EnableUserProfile = false,
                EnableManager = false,
                EnableWorkSettings = false,
                EnableCalendar = true,
            });

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.UserProfile.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Manager.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.WorkSettings.Status);
        AssertFailed(snapshot.Calendar, WorkContextFailureKind.TokenAcquisition);
        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(0, handler.CallCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetSnapshotAsync_WhenTokenIsMissing_ReturnsFailureWithoutSendingHttp(string? accessToken)
    {
        CountingHttpMessageHandler handler = new();
        StaticTokenProvider tokenProvider = new(accessToken);
        Microsoft365WorkContextClient client = CreateClient(
            tokenProvider,
            handler,
            new Microsoft365WorkContextOptions
            {
                EnableUserProfile = true,
                EnableManager = false,
                EnableWorkSettings = false,
                EnableCalendar = true,
            });

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(0, handler.CallCount);
        AssertFailed(snapshot.UserProfile, WorkContextFailureKind.TokenAcquisition);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Manager.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.WorkSettings.Status);
        AssertFailed(snapshot.Calendar, WorkContextFailureKind.TokenAcquisition);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenTransportFails_ReturnsFailureWithoutRetrying()
    {
        HttpRequestException transportException = new(
            "sensitive transport detail",
            inner: new InvalidOperationException("sensitive inner detail"),
            HttpStatusCode.BadGateway);
        ThrowingHttpMessageHandler handler = new(transportException);
        Microsoft365WorkContextClient client = CreateClient(
            new StaticTokenProvider("delegated-token"),
            handler,
            ProfileAndManagerOptions());

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.CallCount);
        AssertFailed(
            snapshot.UserProfile,
            WorkContextFailureKind.Transport,
            HttpStatusCode.BadGateway);
        AssertFailed(
            snapshot.Manager,
            WorkContextFailureKind.Transport,
            HttpStatusCode.BadGateway);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.WorkSettings.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Calendar.Status);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, WorkContextFailureKind.Authentication)]
    [InlineData(HttpStatusCode.Forbidden, WorkContextFailureKind.Authorization)]
    [InlineData(HttpStatusCode.TooManyRequests, WorkContextFailureKind.Throttled)]
    [InlineData(HttpStatusCode.ServiceUnavailable, WorkContextFailureKind.Service)]
    public async Task GetSnapshotAsync_WhenOuterBatchFails_ClassifiesFailureWithoutRetrying(
        HttpStatusCode statusCode,
        WorkContextFailureKind expectedKind)
    {
        using HttpResponseMessage response = new(statusCode)
        {
            Content = new StringContent("sensitive Graph error body"),
        };
        response.Headers.Add("client-request-id", "00000000-0000-4000-8000-000000000016");
        response.Headers.Add("request-id", "00000000-0000-4000-8000-000000000003");
        StaticResponseHttpMessageHandler handler = new(response);
        Microsoft365WorkContextClient client = CreateClient(
            new StaticTokenProvider("delegated-token"),
            handler,
            ProfileAndManagerOptions());

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.CallCount);
        AssertFailed(snapshot.UserProfile, expectedKind, statusCode, "00000000-0000-4000-8000-000000000003");
        AssertFailed(snapshot.Manager, expectedKind, statusCode, "00000000-0000-4000-8000-000000000003");
    }

    [Theory]
    [InlineData("{\"responses\":[")]
    [InlineData("{\"value\":[]}")]
    public async Task GetSnapshotAsync_WhenOuterBatchIsMalformed_ReturnsInvalidResponse(string responseJson)
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson),
        };
        response.Headers.Add("request-id", "00000000-0000-4000-8000-000000000003");
        StaticResponseHttpMessageHandler handler = new(response);
        Microsoft365WorkContextClient client = CreateClient(
            new StaticTokenProvider("delegated-token"),
            handler,
            ProfileAndManagerOptions());

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.CallCount);
        AssertFailed(
            snapshot.UserProfile,
            WorkContextFailureKind.InvalidResponse,
            HttpStatusCode.OK,
            "00000000-0000-4000-8000-000000000003");
        AssertFailed(
            snapshot.Manager,
            WorkContextFailureKind.InvalidResponse,
            HttpStatusCode.OK,
            "00000000-0000-4000-8000-000000000003");
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenProfileBatchOperationFails_PreservesSuccessfulManager()
    {
        using HttpResponseMessage response = BatchResponse("""
            {
              "responses": [
                {
                  "id": "manager",
                  "status": 200,
                  "body": {
                    "displayName": "Morgan Lee",
                    "jobTitle": "Director",
                    "department": "Platform",
                    "officeLocation": "Building 2"
                  }
                },
                {
                  "id": "profile",
                  "status": 403,
                  "headers": { "request-id": "00000000-0000-4000-8000-000000000009" },
                  "body": { "error": { "message": "sensitive Graph error body" } }
                }
              ]
            }
            """);
        Microsoft365WorkContextClient client = CreateClient(
            new StaticTokenProvider("delegated-token"),
            new StaticResponseHttpMessageHandler(response),
            ProfileAndManagerOptions());

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        AssertFailed(
            snapshot.UserProfile,
            WorkContextFailureKind.Authorization,
            HttpStatusCode.Forbidden,
            "00000000-0000-4000-8000-000000000009");
        Assert.Equal(WorkContextFacetStatus.Available, snapshot.Manager.Status);
        Assert.Equal("Morgan Lee", snapshot.Manager.Value?.DisplayName);
        Assert.Null(snapshot.Manager.Failure);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.WorkSettings.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Calendar.Status);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenWorkSettingsChildFails_PreservesSuccessfulAndUnavailableSiblings()
    {
        using HttpResponseMessage response = BatchResponse("""
            {
              "responses": [
                { "id": "work-hours", "status": 204 },
                {
                  "id": "work-language",
                  "status": 429,
                  "headers": { "request-id": "00000000-0000-4000-8000-000000000012" },
                  "body": { "error": { "message": "sensitive Graph error body" } }
                },
                { "id": "manager", "status": 404, "body": {} },
                {
                  "id": "profile",
                  "status": 200,
                  "body": { "displayName": "Avery Ng" }
                },
                { "id": "work-time-zone", "status": 200, "body": "UTC" }
              ]
            }
            """);
        Microsoft365WorkContextClient client = CreateClient(
            new StaticTokenProvider("delegated-token"),
            new StaticResponseHttpMessageHandler(response),
            new Microsoft365WorkContextOptions
            {
                EnableUserProfile = true,
                EnableManager = true,
                EnableWorkSettings = true,
                EnableCalendar = false,
            });

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Available, snapshot.UserProfile.Status);
        Assert.Equal("Avery Ng", snapshot.UserProfile.Value?.DisplayName);
        Assert.Equal(WorkContextFacetStatus.Unavailable, snapshot.Manager.Status);
        Assert.Null(snapshot.Manager.Value);
        Assert.Null(snapshot.Manager.Failure);
        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.WorkSettings.Status);
        Assert.Equal("UTC", snapshot.WorkSettings.Value?.TimeZone);
        Assert.Null(snapshot.WorkSettings.Value?.Language);
        Assert.Null(snapshot.WorkSettings.Value?.WorkingHours);
        WorkContextFacetFailure workSettingsFailure = Assert.IsType<WorkContextFacetFailure>(snapshot.WorkSettings.Failure);
        Assert.Equal(WorkContextFailureKind.Throttled, workSettingsFailure.Kind);
        Assert.Equal(HttpStatusCode.TooManyRequests, workSettingsFailure.StatusCode);
        Assert.Equal("00000000-0000-4000-8000-000000000012", workSettingsFailure.RequestId);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Calendar.Status);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenCalendarBatchOperationFails_PreservesSuccessfulAndUnavailableSiblings()
    {
        using HttpResponseMessage response = BatchResponse("""
            {
              "responses": [
                {
                  "id": "calendar",
                  "status": 503,
                  "headers": { "request-id": "00000000-0000-4000-8000-000000000002" },
                  "body": { "error": { "message": "sensitive Graph error body" } }
                },
                { "id": "manager", "status": 404, "body": {} },
                {
                  "id": "profile",
                  "status": 200,
                  "body": { "displayName": "Avery Ng" }
                }
              ]
            }
            """);
        Microsoft365WorkContextClient client = CreateClient(
            new StaticTokenProvider("delegated-token"),
            new StaticResponseHttpMessageHandler(response),
            new Microsoft365WorkContextOptions
            {
                EnableUserProfile = true,
                EnableManager = true,
                EnableWorkSettings = false,
                EnableCalendar = true,
            });

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Available, snapshot.UserProfile.Status);
        Assert.Equal("Avery Ng", snapshot.UserProfile.Value?.DisplayName);
        Assert.Equal(WorkContextFacetStatus.Unavailable, snapshot.Manager.Status);
        Assert.Null(snapshot.Manager.Value);
        Assert.Null(snapshot.Manager.Failure);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.WorkSettings.Status);
        AssertFailed(
            snapshot.Calendar,
            WorkContextFailureKind.Service,
            HttpStatusCode.ServiceUnavailable,
            "00000000-0000-4000-8000-000000000002");
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenManagerBatchOperationFails_PreservesSuccessfulProfile()
    {
        using HttpResponseMessage response = BatchResponse("""
            {
              "responses": [
                {
                  "id": "manager",
                  "status": 401,
                  "headers": { "request-id": "00000000-0000-4000-8000-000000000007" },
                  "body": { "error": { "message": "sensitive Graph error body" } }
                },
                {
                  "id": "profile",
                  "status": 200,
                  "body": { "displayName": "Avery Ng" }
                }
              ]
            }
            """);
        Microsoft365WorkContextClient client = CreateClient(
            new StaticTokenProvider("delegated-token"),
            new StaticResponseHttpMessageHandler(response),
            ProfileAndManagerOptions());

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Available, snapshot.UserProfile.Status);
        Assert.Equal("Avery Ng", snapshot.UserProfile.Value?.DisplayName);
        AssertFailed(
            snapshot.Manager,
            WorkContextFailureKind.Authentication,
            HttpStatusCode.Unauthorized,
            "00000000-0000-4000-8000-000000000007");
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.WorkSettings.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Calendar.Status);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenCalendarOperationFails_DirectAndBatchFailuresAreEquivalent()
    {
        using HttpResponseMessage directResponse = new(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("sensitive Graph error body"),
        };
        directResponse.Headers.Add("request-id", "00000000-0000-4000-8000-000000000002");
        Microsoft365WorkContextClient directClient = CreateClient(
            new StaticTokenProvider("delegated-token"),
            new StaticResponseHttpMessageHandler(directResponse),
            new Microsoft365WorkContextOptions
            {
                EnableUserProfile = false,
                EnableManager = false,
                EnableWorkSettings = false,
                EnableCalendar = true,
            });

        using HttpResponseMessage batchResponse = BatchResponse("""
            {
              "responses": [
                {
                  "id": "profile",
                  "status": 200,
                  "body": { "displayName": "Avery Ng" }
                },
                {
                  "id": "calendar",
                  "status": 403,
                  "headers": { "request-id": "00000000-0000-4000-8000-000000000002" },
                  "body": { "error": { "message": "sensitive Graph error body" } }
                }
              ]
            }
            """);
        Microsoft365WorkContextClient batchClient = CreateClient(
            new StaticTokenProvider("delegated-token"),
            new StaticResponseHttpMessageHandler(batchResponse),
            new Microsoft365WorkContextOptions
            {
                EnableUserProfile = true,
                EnableManager = false,
                EnableWorkSettings = false,
                EnableCalendar = true,
            });

        WorkContextSnapshot direct = await directClient.GetSnapshotAsync(TestContext.Current.CancellationToken);
        WorkContextSnapshot batch = await batchClient.GetSnapshotAsync(TestContext.Current.CancellationToken);

        AssertFailed(
            direct.Calendar,
            WorkContextFailureKind.Authorization,
            HttpStatusCode.Forbidden,
            "00000000-0000-4000-8000-000000000002");
        AssertFailed(
            batch.Calendar,
            WorkContextFailureKind.Authorization,
            HttpStatusCode.Forbidden,
            "00000000-0000-4000-8000-000000000002");
        Assert.Equal(WorkContextFacetStatus.Disabled, direct.UserProfile.Status);
        Assert.Equal(WorkContextFacetStatus.Available, batch.UserProfile.Status);
    }

    private static Microsoft365WorkContextClient CreateClient(
        IMicrosoft365WorkContextTokenProvider tokenProvider,
        HttpMessageHandler handler,
        Microsoft365WorkContextOptions options) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/") },
            tokenProvider,
            options,
            new FixedTimeProvider(CapturedAtUtc));

    private static Microsoft365WorkContextOptions ProfileAndManagerOptions() =>
        new()
        {
            EnableUserProfile = true,
            EnableManager = true,
            EnableWorkSettings = false,
            EnableCalendar = false,
        };

    private static HttpResponseMessage BatchResponse(string responseJson) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson),
        };

    private static void AssertFailed<T>(
        WorkContextFacetResult<T> result,
        WorkContextFailureKind expectedKind,
        HttpStatusCode? expectedStatusCode = null,
        string? expectedRequestId = null)
        where T : class
    {
        Assert.Equal(WorkContextFacetStatus.Failed, result.Status);
        Assert.Null(result.Value);
        WorkContextFacetFailure failure = Assert.IsType<WorkContextFacetFailure>(result.Failure);
        Assert.Equal(expectedKind, failure.Kind);
        Assert.Equal(expectedStatusCode, failure.StatusCode);
        Assert.Equal(expectedRequestId, failure.RequestId);
    }

    private sealed class ThrowingTokenProvider(Exception exception) : IMicrosoft365WorkContextTokenProvider
    {
        public int CallCount { get; private set; }

        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromException<string>(exception);
        }
    }

    private sealed class StaticTokenProvider(string? accessToken) : IMicrosoft365WorkContextTokenProvider
    {
        public int CallCount { get; private set; }

        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(accessToken!);
        }
    }

    private sealed class CountingHttpMessageHandler : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private sealed class ThrowingHttpMessageHandler(HttpRequestException exception) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromException<HttpResponseMessage>(exception);
        }
    }

    private sealed class StaticResponseHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(response);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
