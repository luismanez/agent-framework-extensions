using System.Text;
using System.Text.Json;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class Microsoft365WorkContextBatchResponseTests
{
    private static readonly DateTimeOffset CapturedAtUtc =
        new(2026, 9, 13, 12, 34, 56, TimeSpan.Zero);

    [Fact]
    public async Task ParseAsync_PreservesSubresponseStatusAndOpaqueBody()
    {
        using StringContent content = JsonContent("""
            {
              "responses": [
                {
                  "id": "profile",
                  "status": 403,
                  "headers": { "request-id": "safe-request-id" },
                  "body": { "error": { "code": "Forbidden", "message": "sensitive" }, "unknown": true },
                  "unknownSubresponseProperty": "ignored"
                }
              ],
              "unknownEnvelopeProperty": "ignored"
            }
            """);

        IReadOnlyList<MicrosoftGraphBatchSubresponse> responses =
            await MicrosoftGraphBatchResponseParser.ParseAsync(
                content,
                TestContext.Current.CancellationToken);

        MicrosoftGraphBatchSubresponse response = Assert.Single(responses);
        Assert.Equal("profile", response.Id);
        Assert.Equal(403, response.Status);
        Assert.Equal("safe-request-id", response.Headers?["request-id"]);
        Assert.Equal(JsonValueKind.Object, response.Body.ValueKind);
        Assert.True(response.Body.TryGetProperty("unknown", out _));
    }

    [Fact]
    public async Task ParseAsync_WhenSubresponseBodyHasUnexpectedShape_LeavesItOpaque()
    {
        using StringContent content = JsonContent("""
            { "responses": [{ "id": "profile", "status": 200, "body": "not-an-object" }] }
            """);

        IReadOnlyList<MicrosoftGraphBatchSubresponse> responses =
            await MicrosoftGraphBatchResponseParser.ParseAsync(
                content,
                TestContext.Current.CancellationToken);

        Assert.Equal(JsonValueKind.String, Assert.Single(responses).Body.ValueKind);
    }

    [Fact]
    public async Task ParseAsync_WhenOuterJsonIsMalformed_ThrowsJsonException()
    {
        using StringContent content = JsonContent("{\"responses\":[");

        await Assert.ThrowsAsync<JsonException>(
            () => MicrosoftGraphBatchResponseParser.ParseAsync(
                content,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ParseAsync_WhenResponsesAreMissing_ThrowsInvalidDataException()
    {
        using StringContent content = JsonContent("{\"value\":[]}");

        await Assert.ThrowsAsync<InvalidDataException>(
            () => MicrosoftGraphBatchResponseParser.ParseAsync(
                content,
                TestContext.Current.CancellationToken));
    }

        [Fact]
        public async Task Correlate_WhenResponsesAreReordered_ReturnsRequestedOrderAndIgnoresUnknownIds()
        {
                IReadOnlyList<MicrosoftGraphOperation> operations = SelectProfileAndManager();
                using StringContent content = JsonContent("""
                        {
                            "responses": [
                                { "id": "unknown", "status": 418, "body": {} },
                                { "id": "manager", "status": 202, "body": {} },
                                { "id": "profile", "status": 201, "body": {} }
                            ]
                        }
                        """);
                IReadOnlyList<MicrosoftGraphBatchSubresponse> responses =
                        await MicrosoftGraphBatchResponseParser.ParseAsync(
                                content,
                                TestContext.Current.CancellationToken);

                IReadOnlyList<MicrosoftGraphCorrelatedResponse> correlated =
                        MicrosoftGraphBatchResponseCorrelator.Correlate(operations, responses);

                Assert.Equal(["profile", "manager"], correlated.Select(item => item.Operation.Id));
                Assert.Equal([201, 202], correlated.Select(item => item.Response?.Status));
                Assert.All(correlated, item => Assert.True(item.IsValid));
        }

        [Fact]
        public async Task Correlate_WhenExpectedIdsAreMissingOrDuplicated_MarksThemInvalid()
        {
                IReadOnlyList<MicrosoftGraphOperation> operations = SelectProfileAndManager();
                using StringContent content = JsonContent("""
                        {
                            "responses": [
                                { "id": "profile", "status": 200, "body": {} },
                                { "id": "profile", "status": 200, "body": {} },
                                { "id": "unknown", "status": 200, "body": {} }
                            ]
                        }
                        """);
                IReadOnlyList<MicrosoftGraphBatchSubresponse> responses =
                        await MicrosoftGraphBatchResponseParser.ParseAsync(
                                content,
                                TestContext.Current.CancellationToken);

                IReadOnlyList<MicrosoftGraphCorrelatedResponse> correlated =
                        MicrosoftGraphBatchResponseCorrelator.Correlate(operations, responses);

                Assert.Equal(2, correlated.Count);
                Assert.All(correlated, item => Assert.False(item.IsValid));
                Assert.All(correlated, item => Assert.Null(item.Response));
        }

        [Fact]
        public async Task GetSnapshotAsync_WhenProfileAndManagerBatchSucceeds_MapsBothBodies()
        {
                StaticBatchResponseHandler handler = new("""
                        {
                            "responses": [
                                {
                                    "id": "manager",
                                    "status": 200,
                                    "body": {
                                        "displayName": "Morgan Lee",
                                        "jobTitle": "Director",
                                        "department": "Platform",
                                        "officeLocation": "Building 2",
                                        "mail": "forbidden@example.com"
                                    }
                                },
                                {
                                    "id": "profile",
                                    "status": 200,
                                    "body": {
                                        "displayName": "Avery Ng",
                                        "givenName": "Avery",
                                        "surname": "Ng",
                                        "jobTitle": "Engineer",
                                        "department": "Platform",
                                        "officeLocation": "Building 1",
                                        "preferredLanguage": "en-US",
                                        "id": "forbidden-id"
                                    }
                                }
                            ]
                        }
                        """);
                Microsoft365WorkContextClient client = new(
                        new HttpClient(handler) { BaseAddress = new Uri("https://graph.microsoft.com/") },
                        new StaticTokenProvider(),
                        new Microsoft365WorkContextOptions
                        {
                                EnableUserProfile = true,
                                EnableManager = true,
                                EnableWorkSettings = false,
                                EnableCalendar = false,
                        },
                        new FixedTimeProvider(CapturedAtUtc));

                WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

                Assert.Equal(WorkContextFacetStatus.Available, snapshot.UserProfile.Status);
                Assert.Equal("Avery Ng", snapshot.UserProfile.Value?.DisplayName);
                Assert.Equal("en-US", snapshot.UserProfile.Value?.PreferredLanguage);
                Assert.Equal(WorkContextFacetStatus.Available, snapshot.Manager.Status);
                Assert.Equal("Morgan Lee", snapshot.Manager.Value?.DisplayName);
                Assert.Equal("Director", snapshot.Manager.Value?.JobTitle);
                Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.WorkSettings.Status);
                Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Calendar.Status);
        }

        private static IReadOnlyList<MicrosoftGraphOperation> SelectProfileAndManager()
        {
                Microsoft365WorkContextOptionsSnapshot options =
                        Microsoft365WorkContextOptionsValidator.ValidateAndSnapshot(
                                new Microsoft365WorkContextOptions
                                {
                                        EnableUserProfile = true,
                                        EnableManager = true,
                                        EnableWorkSettings = false,
                                        EnableCalendar = false,
                                });

                return MicrosoftGraphOperations.Select(options, CapturedAtUtc);
        }

    private static StringContent JsonContent(string json) =>
        new(json, Encoding.UTF8, "application/json");

        private sealed class StaticBatchResponseHandler(string responseJson) : HttpMessageHandler
        {
                protected override Task<HttpResponseMessage> SendAsync(
                        HttpRequestMessage request,
                        CancellationToken cancellationToken) =>
                        Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                        {
                                Content = JsonContent(responseJson),
                        });
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