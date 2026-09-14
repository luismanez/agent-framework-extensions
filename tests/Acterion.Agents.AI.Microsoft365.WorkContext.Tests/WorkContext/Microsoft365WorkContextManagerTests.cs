using System.Net;
using System.Text;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class Microsoft365WorkContextManagerTests
{
    [Fact]
    public async Task GetSnapshotAsync_WhenOnlyManagerEnabled_MapsAllowlistedFieldsFromExactRequest()
    {
        const string AccessToken = "delegated-token";
        RecordingTokenProvider tokenProvider = new(AccessToken);
        RecordingHttpMessageHandler handler = new(HttpStatusCode.OK, """
            {
              "displayName": "Morgan Lee",
              "jobTitle": "Engineering Director",
              "department": "Platform",
              "officeLocation": "Building 2",
              "id": "forbidden-id",
              "mail": "forbidden@example.com",
              "userPrincipalName": "forbidden@example.com",
              "businessPhones": ["+1 555 0100"]
            }
            """);
        Microsoft365WorkContextClient client = CreateClient(
            tokenProvider,
            handler,
            WorkContextErrorBehavior.BestEffort);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Equal(
            "https://graph.microsoft.com/v1.0/me/manager?%24select=displayName%2CjobTitle%2Cdepartment%2CofficeLocation",
            handler.RequestUri?.AbsoluteUri);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal(AccessToken, handler.AuthorizationParameter);

        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.UserProfile.Status);
        Assert.Equal(WorkContextFacetStatus.Available, snapshot.Manager.Status);
        Assert.Null(snapshot.Manager.Failure);
        WorkContextManager manager = Assert.IsType<WorkContextManager>(snapshot.Manager.Value);
        Assert.Equal("Morgan Lee", manager.DisplayName);
        Assert.Equal("Engineering Director", manager.JobTitle);
        Assert.Equal("Platform", manager.Department);
        Assert.Equal("Building 2", manager.OfficeLocation);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.WorkSettings.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Calendar.Status);
    }

    [Theory]
    [InlineData(WorkContextErrorBehavior.BestEffort)]
    [InlineData(WorkContextErrorBehavior.FailFast)]
    public async Task GetSnapshotAsync_WhenManagerIsNotAssigned_ReturnsUnavailable(
        WorkContextErrorBehavior errorBehavior)
    {
        RecordingTokenProvider tokenProvider = new("delegated-token");
        RecordingHttpMessageHandler handler = new(HttpStatusCode.NotFound, """
            { "error": { "code": "Request_ResourceNotFound", "message": "No manager found." } }
            """);
        Microsoft365WorkContextClient client = CreateClient(tokenProvider, handler, errorBehavior);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(WorkContextFacetStatus.Unavailable, snapshot.Manager.Status);
        Assert.Null(snapshot.Manager.Value);
        Assert.Null(snapshot.Manager.Failure);
    }

    private static Microsoft365WorkContextClient CreateClient(
        RecordingTokenProvider tokenProvider,
        RecordingHttpMessageHandler handler,
        WorkContextErrorBehavior errorBehavior) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/") },
            tokenProvider,
            new Microsoft365WorkContextOptions
            {
                EnableUserProfile = false,
                EnableManager = true,
                EnableWorkSettings = false,
                EnableCalendar = false,
                ErrorBehavior = errorBehavior,
            },
            TimeProvider.System);

    private sealed class RecordingTokenProvider(string accessToken) : IMicrosoft365WorkContextTokenProvider
    {
        public int CallCount { get; private set; }

        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(accessToken);
        }
    }

    private sealed class RecordingHttpMessageHandler(HttpStatusCode statusCode, string responseJson)
        : HttpMessageHandler
    {
        public string? AuthorizationParameter { get; private set; }

        public string? AuthorizationScheme { get; private set; }

        public HttpMethod? Method { get; private set; }

        public int RequestCount { get; private set; }

        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            Method = request.Method;
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;

            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
            });
        }
    }
}