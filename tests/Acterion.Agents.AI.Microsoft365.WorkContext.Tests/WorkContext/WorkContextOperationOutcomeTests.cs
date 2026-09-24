using System.Net;
using System.Text.Json;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class WorkContextOperationOutcomeTests
{
    [Theory]
    [InlineData(401, WorkContextFailureKind.Authentication)]
    [InlineData(403, WorkContextFailureKind.Authorization)]
    [InlineData(429, WorkContextFailureKind.Throttled)]
    [InlineData(400, WorkContextFailureKind.Service)]
    [InlineData(418, WorkContextFailureKind.Service)]
    [InlineData(500, WorkContextFailureKind.Service)]
    [InlineData(503, WorkContextFailureKind.Service)]
    public void Classify_WhenGraphReturnsFailure_NormalizesStatus(
        int status,
        WorkContextFailureKind expectedFailureKind)
    {
        MicrosoftGraphOperation operation = Operation("profile", WorkContextFacet.UserProfile);
        MicrosoftGraphBatchSubresponse response = Response(
            operation.Id,
            status,
            """
            { "error": { "code": "unsafe-code", "message": "unsafe-message" } }
            """);

        MicrosoftGraphOperationOutcome outcome = MicrosoftGraphOperationOutcomeClassifier.Classify(
            new MicrosoftGraphCorrelatedResponse(operation, response, IsValid: true));

        Assert.Equal(MicrosoftGraphOperationOutcomeStatus.Failed, outcome.Status);
        Assert.Null(outcome.Payload);
        WorkContextFacetFailure failure = Assert.IsType<WorkContextFacetFailure>(outcome.Failure);
        Assert.Equal(expectedFailureKind, failure.Kind);
        Assert.Equal((HttpStatusCode)status, failure.StatusCode);
    }

    [Fact]
    public void Classify_WhenManagerIsMissing_ReturnsUnavailableWithoutFailure()
    {
        MicrosoftGraphOperation operation = Operation("manager", WorkContextFacet.Manager);
        MicrosoftGraphBatchSubresponse response = Response(
            operation.Id,
            404,
            """{ "error": { "message": "unsafe-message" } }""");

        MicrosoftGraphOperationOutcome outcome = MicrosoftGraphOperationOutcomeClassifier.Classify(
            new MicrosoftGraphCorrelatedResponse(operation, response, IsValid: true));

        Assert.Equal(MicrosoftGraphOperationOutcomeStatus.Unavailable, outcome.Status);
        Assert.Null(outcome.Payload);
        Assert.Null(outcome.Failure);
    }

    [Theory]
    [InlineData("profile", WorkContextFacet.UserProfile)]
    [InlineData("work-time-zone", WorkContextFacet.WorkSettings)]
    [InlineData("calendar", WorkContextFacet.Calendar)]
    public void Classify_WhenUndocumentedOperationReturnsNotFound_ReturnsServiceFailure(
        string operationId,
        WorkContextFacet facet)
    {
        MicrosoftGraphOperation operation = Operation(operationId, facet);
        MicrosoftGraphBatchSubresponse response = Response(operation.Id, 404, "{}");

        MicrosoftGraphOperationOutcome outcome = MicrosoftGraphOperationOutcomeClassifier.Classify(
            new MicrosoftGraphCorrelatedResponse(operation, response, IsValid: true));

        Assert.Equal(MicrosoftGraphOperationOutcomeStatus.Failed, outcome.Status);
        WorkContextFacetFailure failure = Assert.IsType<WorkContextFacetFailure>(outcome.Failure);
        Assert.Equal(WorkContextFailureKind.Service, failure.Kind);
        Assert.Equal(HttpStatusCode.NotFound, failure.StatusCode);
    }

    [Fact]
    public void Classify_WhenSuccessfulBodyIsAnObject_PreservesPayloadWithoutFailurePolicy()
    {
        MicrosoftGraphOperation operation = Operation("profile", WorkContextFacet.UserProfile);
        MicrosoftGraphBatchSubresponse response = Response(
            operation.Id,
            200,
            """{ "displayName": "Avery Ng" }""");

        MicrosoftGraphOperationOutcome outcome = MicrosoftGraphOperationOutcomeClassifier.Classify(
            new MicrosoftGraphCorrelatedResponse(operation, response, IsValid: true));

        Assert.Equal(MicrosoftGraphOperationOutcomeStatus.Succeeded, outcome.Status);
        Assert.Equal("Avery Ng", outcome.Payload?.GetProperty("displayName").GetString());
        Assert.Null(outcome.Failure);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("\"unexpected\"")]
    public void Classify_WhenSuccessfulBodyIsNotAnObject_ReturnsInvalidResponse(string json)
    {
        MicrosoftGraphOperation operation = Operation("profile", WorkContextFacet.UserProfile);
        MicrosoftGraphBatchSubresponse response = Response(operation.Id, 200, json);

        MicrosoftGraphOperationOutcome outcome = MicrosoftGraphOperationOutcomeClassifier.Classify(
            new MicrosoftGraphCorrelatedResponse(operation, response, IsValid: true));

        Assert.Equal(MicrosoftGraphOperationOutcomeStatus.Failed, outcome.Status);
        Assert.Null(outcome.Payload);
        WorkContextFacetFailure failure = Assert.IsType<WorkContextFacetFailure>(outcome.Failure);
        Assert.Equal(WorkContextFailureKind.InvalidResponse, failure.Kind);
        Assert.Equal(HttpStatusCode.OK, failure.StatusCode);
    }

    [Fact]
    public void Classify_WhenCorrelationIsInvalid_ReturnsInvalidResponseWithoutHttpMetadata()
    {
        MicrosoftGraphOperation operation = Operation("profile", WorkContextFacet.UserProfile);

        MicrosoftGraphOperationOutcome outcome = MicrosoftGraphOperationOutcomeClassifier.Classify(
            new MicrosoftGraphCorrelatedResponse(operation, Response: null, IsValid: false));

        Assert.Equal(MicrosoftGraphOperationOutcomeStatus.Failed, outcome.Status);
        Assert.Null(outcome.Payload);
        WorkContextFacetFailure failure = Assert.IsType<WorkContextFacetFailure>(outcome.Failure);
        Assert.Equal(WorkContextFailureKind.InvalidResponse, failure.Kind);
        Assert.Null(failure.StatusCode);
        Assert.Null(failure.RequestId);
    }

    [Fact]
    public void Classify_PrefersRequestIdAndDoesNotRetainRawErrorBody()
    {
        MicrosoftGraphOperation operation = Operation("profile", WorkContextFacet.UserProfile);
        MicrosoftGraphBatchSubresponse response = Response(
            operation.Id,
            403,
            """{ "error": { "code": "forbidden", "message": "private Graph detail" } }""",
            new Dictionary<string, string>
            {
                ["client-request-id"] = "client-request-id",
                ["request-id"] = "graph-request-id",
            });

        MicrosoftGraphOperationOutcome outcome = MicrosoftGraphOperationOutcomeClassifier.Classify(
            new MicrosoftGraphCorrelatedResponse(operation, response, IsValid: true));

        WorkContextFacetFailure failure = Assert.IsType<WorkContextFacetFailure>(outcome.Failure);
        Assert.Equal("graph-request-id", failure.RequestId);
        Assert.Null(outcome.Payload);
    }

    [Fact]
    public void Classify_WhenRequestIdIsAbsent_UsesClientRequestIdCaseInsensitively()
    {
        MicrosoftGraphOperation operation = Operation("profile", WorkContextFacet.UserProfile);
        MicrosoftGraphBatchSubresponse response = Response(
            operation.Id,
            503,
            "{}",
            new Dictionary<string, string> { ["Client-Request-Id"] = "client-request-id" });

        MicrosoftGraphOperationOutcome outcome = MicrosoftGraphOperationOutcomeClassifier.Classify(
            new MicrosoftGraphCorrelatedResponse(operation, response, IsValid: true));

        Assert.Equal("client-request-id", outcome.Failure?.RequestId);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("unsafe\r\nrequest-id")]
    public void Classify_WhenRequestIdIsUnsafe_UsesSafeClientRequestId(string unsafeRequestId)
    {
        MicrosoftGraphOperation operation = Operation("profile", WorkContextFacet.UserProfile);
        MicrosoftGraphBatchSubresponse response = Response(
            operation.Id,
            503,
            "{}",
            new Dictionary<string, string>
            {
                ["request-id"] = unsafeRequestId,
                ["client-request-id"] = "client-request-id",
            });

        MicrosoftGraphOperationOutcome outcome = MicrosoftGraphOperationOutcomeClassifier.Classify(
            new MicrosoftGraphCorrelatedResponse(operation, response, IsValid: true));

        Assert.Equal("client-request-id", outcome.Failure?.RequestId);
    }

    private static MicrosoftGraphOperation Operation(string id, WorkContextFacet facet) =>
        new(
            id,
            facet,
            new Uri("v1.0/me", UriKind.Relative),
            "/me");

    private static MicrosoftGraphBatchSubresponse Response(
        string id,
        int status,
        string json,
        IReadOnlyDictionary<string, string>? headers = null)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return new MicrosoftGraphBatchSubresponse(id, status, headers, document.RootElement.Clone());
    }
}
