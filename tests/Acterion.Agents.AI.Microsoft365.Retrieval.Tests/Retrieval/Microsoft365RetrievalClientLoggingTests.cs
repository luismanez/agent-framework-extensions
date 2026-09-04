using System.Net;
using System.Text;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class Microsoft365RetrievalClientLoggingTests
{
    [Fact]
    public async Task RetrieveAsync_LogsOnlyAllowlistedDimensionsForSuccessfulRetrieval()
    {
        const string query = "quarterly plan with sensitive words";
        const string filter = "path:secret-filter-value";
        const string extract = "retrieved document content";
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                "{\"retrievalHits\":[{\"webUrl\":\"https://contoso.sharepoint.com/document\",\"extracts\":[{\"text\":\"" + extract + "\"}],\"resourceMetadata\":{\"title\":\"secret-metadata-value\"}}]}",
                Encoding.UTF8,
                "application/json"),
        };
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler);
        CollectingLogger<Microsoft365RetrievalClient> logger = new();
        Microsoft365RetrievalClient client = new(
            httpClient,
            new StubTokenProvider("delegated-token"),
            new Microsoft365RetrievalOptions { FilterExpression = filter },
            logger);

        await client.RetrieveAsync(query, TestContext.Current.CancellationToken);

        Assert.Collection(
            logger.Entries,
            entry =>
            {
                Assert.Equal(1000, entry.EventId.Id);
                Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, entry.Level);
            },
            entry =>
            {
                Assert.Equal(1001, entry.EventId.Id);
                Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, entry.Level);
            });
        string logText = string.Join(Environment.NewLine, logger.Entries.Select(entry => entry.Message));
        Assert.DoesNotContain(query, logText, StringComparison.Ordinal);
        Assert.DoesNotContain(filter, logText, StringComparison.Ordinal);
        Assert.DoesNotContain(extract, logText, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-metadata-value", logText, StringComparison.Ordinal);
        Assert.DoesNotContain("delegated-token", logText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RetrieveAsync_LogsThrottlingWithoutFailedOrCompletedEvent()
    {
        using HttpResponseMessage response = new(HttpStatusCode.TooManyRequests);
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler);
        CollectingLogger<Microsoft365RetrievalClient> logger = new();
        Microsoft365RetrievalClient client = new(
            httpClient,
            new StubTokenProvider(),
            new Microsoft365RetrievalOptions(),
            logger);

        await Assert.ThrowsAsync<Microsoft365RetrievalException>(
            () => client.RetrieveAsync("quarterly plan", TestContext.Current.CancellationToken));

        Assert.Equal([1000, 1003], logger.Entries.Select(entry => entry.EventId.Id));
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Warning, logger.Entries[1].Level);
    }

    [Fact]
    public async Task RetrieveAsync_LogsFailureWithoutSensitiveTransportExceptionDetails()
    {
        using ThrowingHttpMessageHandler handler = new(new HttpRequestException("token=delegated-token"));
        using HttpClient httpClient = new(handler);
        CollectingLogger<Microsoft365RetrievalClient> logger = new();
        Microsoft365RetrievalClient client = new(
            httpClient,
            new StubTokenProvider(),
            new Microsoft365RetrievalOptions(),
            logger);

        await Assert.ThrowsAsync<Microsoft365RetrievalException>(
            () => client.RetrieveAsync("quarterly plan", TestContext.Current.CancellationToken));

        Assert.Equal([1000, 1002], logger.Entries.Select(entry => entry.EventId.Id));
        Assert.DoesNotContain("delegated-token", logger.Entries[1].Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RetrieveAsync_DoesNotLogFailureOrCompletionForCancellation()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        using HttpResponseMessage response = new(HttpStatusCode.OK);
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler);
        CollectingLogger<Microsoft365RetrievalClient> logger = new();
        Microsoft365RetrievalClient client = new(
            httpClient,
            new CancelingTokenProvider(),
            new Microsoft365RetrievalOptions(),
            logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.RetrieveAsync("quarterly plan", cancellation.Token));

        Assert.Equal([1000], logger.Entries.Select(entry => entry.EventId.Id));
    }

    private sealed class CancelingTokenProvider : IMicrosoft365RetrievalTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromCanceled<string>(cancellationToken);
    }
}
