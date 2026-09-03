using System.Net;
using System.Text.Json;
using Acterion.Agents.AI.Microsoft365.Retrieval;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class PublicContractTests
{
    [Fact]
    public async Task CoreContracts_AreConsumableWithSpecifiedDefaultsAndSignatures()
    {
        Microsoft365RetrievalOptions options = new();
        IMicrosoft365RetrievalTokenProvider tokenProvider = new StubTokenProvider();
        IMicrosoft365RetrievalClient client = new StubRetrievalClient();
        using CancellationTokenSource cancellation = new();

        string token = await tokenProvider.GetAccessTokenAsync(cancellation.Token);
        IReadOnlyList<Microsoft365RetrievalHit> hits =
            await client.RetrieveAsync("quarterly report", cancellation.Token);

        Assert.Equal(8, options.MaximumNumberOfResults);
        Assert.Null(options.FilterExpression);
        Assert.Equal(["title", "author"], options.ResourceMetadata);
        Assert.Equal("token", token);
        Assert.Empty(hits);
    }

    [Fact]
    public void CoreContracts_ExposeOnlySpecifiedMembers()
    {
        Assert.True(typeof(Microsoft365RetrievalOptions).IsSealed);
        Assert.Equal(
            ["FilterExpression", "MaximumNumberOfResults", "ResourceMetadata"],
            typeof(Microsoft365RetrievalOptions)
                .GetProperties()
                .Select(property => property.Name)
                .Order());

        AssertSingleMethod(
            typeof(IMicrosoft365RetrievalTokenProvider),
            "GetAccessTokenAsync",
            typeof(Task<string>),
            typeof(CancellationToken));
        AssertSingleMethod(
            typeof(IMicrosoft365RetrievalClient),
            "RetrieveAsync",
            typeof(Task<IReadOnlyList<Microsoft365RetrievalHit>>),
            typeof(string),
            typeof(CancellationToken));
    }

    [Fact]
    public void ResultContracts_ExposeOnlySpecifiedReadOnlyMembers()
    {
        Assert.True(typeof(Microsoft365RetrievalHit).IsSealed);
        Assert.Empty(typeof(Microsoft365RetrievalHit).GetConstructors());
        AssertReadOnlyProperties(
            typeof(Microsoft365RetrievalHit),
            ("Extracts", typeof(IReadOnlyList<Microsoft365RetrievalExtract>)),
            ("ResourceMetadata", typeof(IReadOnlyDictionary<string, JsonElement>)),
            ("ResourceType", typeof(string)),
            ("WebUrl", typeof(string)));

        Assert.True(typeof(Microsoft365RetrievalExtract).IsSealed);
        Assert.Empty(typeof(Microsoft365RetrievalExtract).GetConstructors());
        AssertReadOnlyProperties(
            typeof(Microsoft365RetrievalExtract),
            ("RelevanceScore", typeof(double?)),
            ("Text", typeof(string)));

        Assert.True(typeof(Microsoft365RetrievalException).IsSealed);
        Assert.True(typeof(Exception).IsAssignableFrom(typeof(Microsoft365RetrievalException)));
        AssertReadOnlyProperties(
            typeof(Microsoft365RetrievalException),
            ("RequestId", typeof(string)),
            ("StatusCode", typeof(HttpStatusCode?)));

        InvalidOperationException innerException = new();
        Microsoft365RetrievalException exception = new(
            "Retrieval failed.",
            HttpStatusCode.BadRequest,
            "request-id",
            innerException);
        Assert.Same(innerException, exception.InnerException);
    }

    private static void AssertReadOnlyProperties(
        Type contractType,
        params (string Name, Type Type)[] expectedProperties)
    {
        (string Name, Type Type, bool CanWrite)[] actualProperties = contractType
            .GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .Where(property => property.DeclaringType == contractType)
            .Select(property => (property.Name, property.PropertyType, property.CanWrite))
            .OrderBy(property => property.Name)
            .ToArray();

        Assert.Equal(
            expectedProperties,
            actualProperties.Select(property => (property.Name, property.Type)));
        Assert.All(actualProperties, property => Assert.False(property.CanWrite));
    }

    private static void AssertSingleMethod(
        Type contractType,
        string name,
        Type returnType,
        params Type[] parameterTypes)
    {
        System.Reflection.MethodInfo method = Assert.Single(contractType.GetMethods());

        Assert.Equal(name, method.Name);
        Assert.Equal(returnType, method.ReturnType);
        Assert.Equal(
            parameterTypes,
            method.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.True(method.GetParameters()[^1].HasDefaultValue);
        Assert.Null(method.GetParameters()[^1].DefaultValue);
    }

    private sealed class StubTokenProvider : IMicrosoft365RetrievalTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("token");
    }

    private sealed class StubRetrievalClient : IMicrosoft365RetrievalClient
    {
        public Task<IReadOnlyList<Microsoft365RetrievalHit>> RetrieveAsync(
            string query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Microsoft365RetrievalHit>>([]);
    }
}