using System.Net;
using System.Text;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class Microsoft365RetrievalClientResponseTests
{
    [Fact]
    public async Task RetrieveAsync_MapsHitsExtractsAndScalarMetadataInResponseOrder()
    {
        using HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {
                  "retrievalHits": [
                    {
                      "webUrl": "https://contoso.sharepoint.com/sites/Engineering/plan.docx",
                      "extracts": [
                        { "text": "First extract", "relevanceScore": 0.91 },
                        { "text": "Second extract" }
                      ],
                      "resourceType": "document",
                      "resourceMetadata": {
                        "title": "Engineering plan",
                        "rank": 2,
                        "isCurrent": true,
                        "owner": null
                      },
                      "unknownField": "ignored"
                    },
                    {
                      "webUrl": "https://contoso.sharepoint.com/sites/Engineering/roadmap.docx",
                      "extracts": [],
                      "resourceMetadata": {}
                    }
                  ],
                  "unknownResponseField": "ignored"
                }
                """,
                Encoding.UTF8,
                "application/json"),
        };
        using RecordingHttpMessageHandler handler = new(response);
        using HttpClient httpClient = new(handler);
        Microsoft365RetrievalClient client = new(
            httpClient,
            new StubTokenProvider(),
            new Microsoft365RetrievalOptions());

        IReadOnlyList<Microsoft365RetrievalHit> hits = await client.RetrieveAsync(
            "engineering plan",
            TestContext.Current.CancellationToken);

        Assert.Equal(2, hits.Count);

        Microsoft365RetrievalHit first = hits[0];
        Assert.Equal("https://contoso.sharepoint.com/sites/Engineering/plan.docx", first.WebUrl);
        Assert.Equal("document", first.ResourceType);
        Assert.Equal(["First extract", "Second extract"], first.Extracts.Select(extract => extract.Text));
        Assert.Equal(0.91d, first.Extracts[0].RelevanceScore);
        Assert.Null(first.Extracts[1].RelevanceScore);
        Assert.Equal("Engineering plan", first.ResourceMetadata["title"].GetString());
        Assert.Equal(2, first.ResourceMetadata["rank"].GetInt32());
        Assert.True(first.ResourceMetadata["isCurrent"].GetBoolean());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, first.ResourceMetadata["owner"].ValueKind);

        Microsoft365RetrievalHit second = hits[1];
        Assert.Equal("https://contoso.sharepoint.com/sites/Engineering/roadmap.docx", second.WebUrl);
        Assert.Null(second.ResourceType);
        Assert.Empty(second.Extracts);
        Assert.Empty(second.ResourceMetadata);
    }

      [Fact]
      public async Task RetrieveAsync_MapsMissingOptionalCollectionsToEmptyCollections()
      {
        Microsoft365RetrievalClient client = CreateClient(
          """
          {
            "retrievalHits": [
            { "webUrl": "https://contoso.sharepoint.com/sites/Engineering/plan.docx" }
            ]
          }
          """);

        Microsoft365RetrievalHit hit = Assert.Single(await client.RetrieveAsync(
          "engineering plan",
          TestContext.Current.CancellationToken));

        Assert.Empty(hit.Extracts);
        Assert.Empty(hit.ResourceMetadata);
        Assert.Null(hit.ResourceType);
      }

      [Theory]
      [InlineData("{")]
      [InlineData("{}")]
      [InlineData("""{"retrievalHits":[{}]}""")]
      [InlineData("""{"retrievalHits":[{"webUrl":"https://contoso.sharepoint.com/plan.docx","extracts":[{}]}]}""")]
      [InlineData("""{"retrievalHits":[{"webUrl":"https://contoso.sharepoint.com/plan.docx","resourceMetadata":{"title":{}}}]}""")]
      [InlineData("""{"retrievalHits":[{"webUrl":"https://contoso.sharepoint.com/plan.docx","resourceMetadata":{"tags":[]}}]}""")]
      public async Task RetrieveAsync_WrapsMalformedSuccessfulPayload(string responseBody)
      {
        Microsoft365RetrievalClient client = CreateClient(responseBody);

        Microsoft365RetrievalException exception = await Assert.ThrowsAsync<Microsoft365RetrievalException>(
          () => client.RetrieveAsync("engineering plan", TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.OK, exception.StatusCode);
        Assert.IsType<System.Text.Json.JsonException>(exception.InnerException);
      }

      private static Microsoft365RetrievalClient CreateClient(string responseBody)
      {
        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
          Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
        };
        RecordingHttpMessageHandler handler = new(response);
        HttpClient httpClient = new(handler);

        return new Microsoft365RetrievalClient(
          httpClient,
          new StubTokenProvider(),
          new Microsoft365RetrievalOptions());
      }
}
