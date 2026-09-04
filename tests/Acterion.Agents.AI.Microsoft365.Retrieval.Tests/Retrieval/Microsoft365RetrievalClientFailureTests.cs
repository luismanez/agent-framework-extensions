using System.Net;
using System.Text;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class Microsoft365RetrievalClientFailureTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "The retrieval request was rejected.")]
    [InlineData(HttpStatusCode.Unauthorized, "The delegated access token was rejected.")]
    [InlineData(HttpStatusCode.Forbidden, "The delegated user is not authorized to retrieve this content.")]
    [InlineData(HttpStatusCode.TooManyRequests, "Microsoft Graph throttled the retrieval request.")]
    [InlineData(HttpStatusCode.InternalServerError, "Microsoft Graph is temporarily unavailable.")]
    [InlineData(HttpStatusCode.NotFound, "Microsoft Graph returned an unsuccessful retrieval response.")]
    public async Task RetrieveAsync_MapsGraphFailuresToSafePackageException(
        HttpStatusCode statusCode,
        string expectedMessage)
    {
        using HttpResponseMessage response = new(statusCode)
        {
            Content = new StringContent(
                "token=delegated-token query=engineering plan retrieved document content",
                Encoding.UTF8,
                "text/plain"),
        };
        response.Headers.Add("client-request-id", "client-request-id");
        response.Headers.Add("request-id", "request-id");
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler);
        Microsoft365RetrievalClient client = new(
            httpClient,
            new StubTokenProvider(),
            new Microsoft365RetrievalOptions());

        Microsoft365RetrievalException exception = await Assert.ThrowsAsync<Microsoft365RetrievalException>(
            () => client.RetrieveAsync("engineering plan", TestContext.Current.CancellationToken));

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Equal("request-id", exception.RequestId);
        Assert.Equal(expectedMessage, exception.Message);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain("delegated-token", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("engineering plan", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("retrieved document content", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task RetrieveAsync_UsesClientRequestIdWhenGraphRequestIdIsAbsent()
    {
        using HttpResponseMessage response = new(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("sensitive error body", Encoding.UTF8, "text/plain"),
        };
        response.Headers.Add("client-request-id", "client-request-id");
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler);
        Microsoft365RetrievalClient client = new(
            httpClient,
            new StubTokenProvider(),
            new Microsoft365RetrievalOptions());

        Microsoft365RetrievalException exception = await Assert.ThrowsAsync<Microsoft365RetrievalException>(
            () => client.RetrieveAsync("engineering plan", TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.BadGateway, exception.StatusCode);
        Assert.Equal("client-request-id", exception.RequestId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task RetrieveAsync_WrapsTransportFailureAndPreservesStatus(
        HttpStatusCode? statusCode)
    {
        HttpRequestException transportException = new("transport failure", null, statusCode);
        using ThrowingHttpMessageHandler handler = new(transportException);
        using HttpClient httpClient = new(handler);
        Microsoft365RetrievalClient client = new(
            httpClient,
            new StubTokenProvider(),
            new Microsoft365RetrievalOptions());

        Microsoft365RetrievalException exception = await Assert.ThrowsAsync<Microsoft365RetrievalException>(
            () => client.RetrieveAsync("engineering plan", TestContext.Current.CancellationToken));

        Assert.Equal(statusCode, exception.StatusCode);
        Assert.Null(exception.RequestId);
        Assert.Equal("The retrieval request could not reach Microsoft Graph.", exception.Message);
        Assert.Same(transportException, exception.InnerException);
        Assert.Equal(1, handler.RequestCount);
    }
}
