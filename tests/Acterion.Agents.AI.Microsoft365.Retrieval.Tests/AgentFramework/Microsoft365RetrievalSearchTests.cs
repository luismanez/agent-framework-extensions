using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class Microsoft365RetrievalSearchTests
{
    [Fact]
    public async Task SearchAsync_ForwardsInputsAndMapsHitsInReceivedOrder()
    {
        Microsoft365RetrievalHit firstHit = CreateHit(
            "https://contoso.sharepoint.com/sites/finance/Q1%20Report.docx",
            [CreateExtract("First extract"), CreateExtract("  "), CreateExtract("Second extract")],
            [("title", "Quarterly report"), ("sensitivityLabel", "Confidential")]);
        Microsoft365RetrievalHit secondHit = CreateHit(
            "https://contoso.sharepoint.com/sites/finance/Budget.xlsx",
            [CreateExtract("Budget extract")],
            []);
        StubRetrievalClient client = new([firstHit, secondHit]);
        Microsoft365RetrievalSearch search = new(client);
        using CancellationTokenSource cancellation = new();

        TextSearchProvider.TextSearchResult[] results = (await search.SearchAsync(
            "quarterly plan",
            cancellation.Token)).ToArray();

        Assert.Equal("quarterly plan", client.Query);
        Assert.Equal(cancellation.Token, client.CancellationToken);
        Assert.Equal(2, results.Length);
        Assert.Equal("Quarterly report", results[0].SourceName);
        Assert.Equal(firstHit.WebUrl, results[0].SourceLink);
        Assert.Equal("First extract\nSecond extract", results[0].Text);
        Assert.Same(firstHit, results[0].RawRepresentation);
        Assert.DoesNotContain("Confidential", results[0].Text);
        Assert.Equal("Budget.xlsx", results[1].SourceName);
        Assert.Same(secondHit, results[1].RawRepresentation);
    }

    [Fact]
    public async Task SearchAsync_UsesOriginalUrlForAnUnusableSourceNameAndEmptyTextForEmptyExtracts()
    {
        Microsoft365RetrievalHit hit = CreateHit(
            "not a uri",
            [CreateExtract(" "), CreateExtract("\t")],
            [("title", " ")]);
        Microsoft365RetrievalSearch search = new(new StubRetrievalClient([hit]));

        TextSearchProvider.TextSearchResult result = Assert.Single(await search.SearchAsync(
            "query",
            CancellationToken.None));

        Assert.Equal(hit.WebUrl, result.SourceName);
        Assert.Equal(string.Empty, result.Text);
        Assert.Same(hit, result.RawRepresentation);
    }

    [Fact]
    public async Task SearchAsync_ReturnsAnEmptySequenceForNoHits()
    {
        Microsoft365RetrievalSearch search = new(new StubRetrievalClient([]));

        IEnumerable<TextSearchProvider.TextSearchResult> results = await search.SearchAsync(
            "query",
            CancellationToken.None);

        Assert.Empty(results);
    }

    private static Microsoft365RetrievalHit CreateHit(
        string webUrl,
        IReadOnlyList<Microsoft365RetrievalExtract> extracts,
        IReadOnlyList<(string Name, string Value)> metadata)
    {
        using JsonDocument document = JsonDocument.Parse(
            JsonSerializer.Serialize(metadata.ToDictionary(item => item.Name, item => item.Value)));
        Dictionary<string, JsonElement> elements = document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value);

        return Create<Microsoft365RetrievalHit>(webUrl, extracts, null, elements);
    }

    private static Microsoft365RetrievalExtract CreateExtract(string text) =>
        Create<Microsoft365RetrievalExtract>(text, null);

    private static T Create<T>(params object?[] arguments)
    {
        ConstructorInfo constructor = Assert.Single(
            typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic));

        try
        {
            return (T)constructor.Invoke(arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private sealed class StubRetrievalClient(IReadOnlyList<Microsoft365RetrievalHit> hits)
        : IMicrosoft365RetrievalClient
    {
        public string? Query { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public Task<IReadOnlyList<Microsoft365RetrievalHit>> RetrieveAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            Query = query;
            CancellationToken = cancellationToken;
            return Task.FromResult(hits);
        }
    }
}