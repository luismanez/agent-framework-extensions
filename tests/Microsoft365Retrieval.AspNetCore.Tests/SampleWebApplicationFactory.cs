using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Agents.AI;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft365Retrieval.AspNetCore.Tests;

public sealed class SampleWebApplicationFactory : WebApplicationFactory<Program>
{
    internal StubChatClient ChatClient { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(TestAuthenticationHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });
            services.RemoveAll<AIAgent>();
            services.AddSingleton<AIAgent>(ChatClient.AsAIAgent(
                instructions: "Test agent.",
                name: "TestAgent"));
        });
    }
}

internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.Authorization.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, "Test user")], SchemeName);
        AuthenticationTicket ticket = new(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

internal sealed class StubChatClient : IChatClient
{
    private readonly Func<ChatResponse> responseFactory;

    public StubChatClient(Func<ChatResponse>? responseFactory = null)
    {
        this.responseFactory = responseFactory ?? (() => new ChatResponse(new ChatMessage(ChatRole.Assistant, "Grounded answer.")));
    }

    public CancellationToken LastCancellationToken { get; private set; }

    public int ResponseCallCount { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        LastCancellationToken = cancellationToken;
        ResponseCallCount++;
        return Task.FromResult(responseFactory());
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LastCancellationToken = cancellationToken;
        ChatResponse response = responseFactory();

        foreach (ChatMessage message in response.Messages)
        {
            yield return new ChatResponseUpdate(message.Role, message.Contents)
            {
                FinishReason = ChatFinishReason.Stop,
            };
        }

        await Task.CompletedTask;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}