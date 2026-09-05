using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Microsoft365Retrieval.AspNetCore.Tests;

public sealed class AssistantEndpointTests : IClassFixture<SampleWebApplicationFactory>
{
    private readonly SampleWebApplicationFactory factory;

    public AssistantEndpointTests(SampleWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task PostAssistant_WithoutAuthentication_IsChallenged()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/assistant", new { message = "Find the policy." });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostAssistant_WithWhitespaceMessage_ReturnsValidationProblemDetails()
    {
        using HttpClient client = CreateAuthenticatedClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/assistant", new { message = " " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task PostAssistant_WithValidMessage_ReturnsTheAgentAnswer()
    {
        using HttpClient client = CreateAuthenticatedClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/assistant", new { message = "Find the policy." });

        response.EnsureSuccessStatusCode();
        AssistantResponse? responseBody = await response.Content.ReadFromJsonAsync<AssistantResponse>();
        Assert.Equal("Grounded answer.", responseBody!.Answer);
        Assert.Equal(1, factory.ChatClient.ResponseCallCount);
        Assert.True(factory.ChatClient.LastCancellationToken.CanBeCanceled);
    }

    [Fact]
    public async Task PostAssistant_WhenAgentFails_ReturnsSafeProblemDetails()
    {
        using HttpClient client = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<AIAgent>();
                services.AddSingleton<AIAgent>(new StubChatClient(
                    () => throw new InvalidOperationException("sensitive failure details"))
                    .AsAIAgent(instructions: "Test agent.", name: "FailingTestAgent"));
            }))
            .CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthenticationHandler.SchemeName);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/assistant", new { message = "Find the policy." });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        string responseBody = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("sensitive failure details", responseBody, StringComparison.Ordinal);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthenticationHandler.SchemeName);
        return client;
    }

    private sealed record AssistantResponse(string Answer);
}