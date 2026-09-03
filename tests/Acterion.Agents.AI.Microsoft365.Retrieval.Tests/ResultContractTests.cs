using System.Collections;
using System.Net;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Acterion.Agents.AI.Microsoft365.Retrieval;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class ResultContractTests
{
    [Fact]
    public void Hit_CopiesCollectionsAndClonesScalarMetadata()
    {
        using JsonDocument document = JsonDocument.Parse(
            """{"title":"Report","count":3,"published":true,"owner":null}""");
        Dictionary<string, JsonElement> metadata = document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value);
        List<Microsoft365RetrievalExtract> extracts =
        [
            Create<Microsoft365RetrievalExtract>("first", 0.9d),
            Create<Microsoft365RetrievalExtract>("second", null),
        ];

        Microsoft365RetrievalHit hit = Create<Microsoft365RetrievalHit>(
            "https://contoso.sharepoint.com/report.docx",
            extracts,
            "document",
            metadata);

        extracts.Clear();
        metadata.Clear();
        document.Dispose();

        Assert.Equal("https://contoso.sharepoint.com/report.docx", hit.WebUrl);
        Assert.Equal("document", hit.ResourceType);
        Assert.Equal(["first", "second"], hit.Extracts.Select(extract => extract.Text));
        Assert.Equal(0.9d, hit.Extracts[0].RelevanceScore);
        Assert.Null(hit.Extracts[1].RelevanceScore);
        Assert.Equal("Report", hit.ResourceMetadata["title"].GetString());
        Assert.Equal(3, hit.ResourceMetadata["count"].GetInt32());
        Assert.True(hit.ResourceMetadata["published"].GetBoolean());
        Assert.Equal(JsonValueKind.Null, hit.ResourceMetadata["owner"].ValueKind);
        Assert.False(hit.ResourceMetadata.ContainsKey("TITLE"));
        Assert.Throws<NotSupportedException>(
            () => ((IList)hit.Extracts).Add(Create<Microsoft365RetrievalExtract>("third", 0.1d)));
        Assert.Throws<NotSupportedException>(
            () => ((IDictionary)hit.ResourceMetadata).Add("new", default(JsonElement)));
    }

    [Theory]
    [InlineData("{\"nested\":{}}")]
    [InlineData("{\"nested\":[]}")]
    public void Hit_RejectsNonScalarMetadata(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        Dictionary<string, JsonElement> metadata = document.RootElement
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => Create<Microsoft365RetrievalHit>(
                "https://contoso.sharepoint.com/report.docx",
                Array.Empty<Microsoft365RetrievalExtract>(),
                null,
                metadata));

        Assert.Equal("resourceMetadata", exception.ParamName);
    }

    [Fact]
    public void PackageException_PreservesInspectableDiagnosticsAndInnerException()
    {
        InvalidOperationException innerException = new("transport failed");

        Microsoft365RetrievalException exception = new(
            "Retrieval failed.",
            HttpStatusCode.TooManyRequests,
            "request-123",
            innerException);

        Assert.Equal("Retrieval failed.", exception.Message);
        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
        Assert.Equal("request-123", exception.RequestId);
        Assert.Same(innerException, exception.InnerException);
    }

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
}