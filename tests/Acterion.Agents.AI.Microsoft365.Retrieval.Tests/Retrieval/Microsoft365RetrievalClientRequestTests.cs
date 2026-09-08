using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class Microsoft365RetrievalClientRequestTests
{
    [Fact]
    public async Task RetrieveAsync_SendsConfiguredSharePointRequestAndReturnsEmptyHits()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"retrievalHits":[]}""", Encoding.UTF8, "application/json"),
        };
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri("https://graph.microsoft.com/"),
        };
        StubTokenProvider tokenProvider = new();
        Microsoft365RetrievalOptions options = new()
        {
            MaximumNumberOfResults = 12,
            ResourceMetadata = ["title", "author", "createdDateTime"],
        };
        Microsoft365RetrievalClient client = new(httpClient, tokenProvider, options);
        using CancellationTokenSource cancellation = new();

        IReadOnlyList<Microsoft365RetrievalHit> hits = await client.RetrieveAsync(
            "quarterly plan",
            cancellation.Token);

        Assert.Empty(hits);
        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(cancellation.Token, tokenProvider.CancellationToken);
        Assert.Equal(1, handler.RequestCount);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(
            new Uri("https://graph.microsoft.com/v1.0/copilot/retrieval"),
            handler.RequestUri);
        Assert.Equal("Bearer", handler.Authorization?.Scheme);
        Assert.Equal("delegated-token", handler.Authorization?.Parameter);
        Assert.Equal("application/json", handler.ContentType);
        Assert.Equal("utf-8", handler.Charset);

        using JsonDocument request = JsonDocument.Parse(Assert.IsType<string>(handler.Content));
        JsonElement root = request.RootElement;
        Assert.Equal(4, root.EnumerateObject().Count());
        Assert.Equal("quarterly plan", root.GetProperty("queryString").GetString());
        Assert.Equal("sharePoint", root.GetProperty("dataSource").GetString());
        Assert.Equal(12, root.GetProperty("maximumNumberOfResults").GetInt32());
        Assert.Equal(
            ["title", "author", "createdDateTime"],
            root.GetProperty("resourceMetadata")
                .EnumerateArray()
                .Select(element => element.GetString()));
        Assert.False(root.TryGetProperty("filterExpression", out _));
    }

    [Fact]
    public async Task RetrieveAsync_AcceptsMaximumQueryLengthAndIncludesConfiguredFilter()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"retrievalHits":[]}""", Encoding.UTF8, "application/json"),
        };
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler);
        StubTokenProvider tokenProvider = new();
        Microsoft365RetrievalClient client = new(
            httpClient,
            tokenProvider,
            new Microsoft365RetrievalOptions
            {
                FilterExpression = "path:\"https://contoso.sharepoint.com/sites/Engineering/\"",
            });
        string query = new('q', 1_500);

        IReadOnlyList<Microsoft365RetrievalHit> hits = await client.RetrieveAsync(
            query,
            TestContext.Current.CancellationToken);

        Assert.Empty(hits);
        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(1, handler.RequestCount);
        using JsonDocument request = JsonDocument.Parse(Assert.IsType<string>(handler.Content));
        Assert.Equal(query, request.RootElement.GetProperty("queryString").GetString());
        Assert.Equal(
            "path:\"https://contoso.sharepoint.com/sites/Engineering/\"",
            request.RootElement.GetProperty("filterExpression").GetString());
    }

    [Fact]
    public async Task RetrieveAsync_SerializesTypedFilterExpressionWithoutAdditionalRequests()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"retrievalHits":[]}""", Encoding.UTF8, "application/json"),
        };
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler);
        StubTokenProvider tokenProvider = new();
        SharePointRetrievalFilter filter = SharePointRetrievalFilter.AllOf(
            SharePointRetrievalFilter.SiteId(
                Guid.Parse("f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6")),
            SharePointRetrievalFilter.FileExtensions("pdf", "docx"),
            SharePointRetrievalFilter.LastModifiedOnOrAfter(
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        Microsoft365RetrievalClient client = new(
            httpClient,
            tokenProvider,
            new Microsoft365RetrievalOptions { FilterExpression = filter.Expression });

        IReadOnlyList<Microsoft365RetrievalHit> hits = await client.RetrieveAsync(
            "engineering plans",
            TestContext.Current.CancellationToken);

        Assert.Empty(hits);
        Assert.Equal(1, tokenProvider.CallCount);
        Assert.Equal(1, handler.RequestCount);
        using JsonDocument request = JsonDocument.Parse(Assert.IsType<string>(handler.Content));
        Assert.Equal(filter.Expression, request.RootElement.GetProperty("filterExpression").GetString());
    }

    [Fact]
    public async Task RetrieveAsync_RejectsNullQueryBeforeTokenOrHttp()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK);
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler);
        StubTokenProvider tokenProvider = new();
        Microsoft365RetrievalClient client = new(
            httpClient,
            tokenProvider,
            new Microsoft365RetrievalOptions());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.RetrieveAsync(null!, TestContext.Current.CancellationToken));

        Assert.Equal(0, tokenProvider.CallCount);
        Assert.Equal(0, handler.RequestCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public async Task RetrieveAsync_RejectsEmptyOrWhitespaceQueryBeforeTokenOrHttp(string query)
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK);
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler);
        StubTokenProvider tokenProvider = new();
        Microsoft365RetrievalClient client = new(
            httpClient,
            tokenProvider,
            new Microsoft365RetrievalOptions());

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => client.RetrieveAsync(query, TestContext.Current.CancellationToken));

        Assert.Equal("query", exception.ParamName);
        Assert.Equal(0, tokenProvider.CallCount);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task RetrieveAsync_RejectsQueryOverMaximumLengthBeforeTokenOrHttp()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK);
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler);
        StubTokenProvider tokenProvider = new();
        Microsoft365RetrievalClient client = new(
            httpClient,
            tokenProvider,
            new Microsoft365RetrievalOptions());

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => client.RetrieveAsync(new string('q', 1_501), TestContext.Current.CancellationToken));

        Assert.Equal("query", exception.ParamName);
        Assert.Equal(0, tokenProvider.CallCount);
        Assert.Equal(0, handler.RequestCount);
    }
}
