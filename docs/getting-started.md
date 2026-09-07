# Getting started

This guide takes a .NET host from prerequisites to its first permission-trimmed SharePoint retrieval call. You can stop after direct retrieval or continue by adding the same retrieval service to a Microsoft Agent Framework agent.

## 1. Check the prerequisites

You need:

- .NET 10 SDK.
- A Microsoft Entra work or school tenant.
- SharePoint Online content accessible to the test user.
- A Microsoft Entra app registration configured for a delegated host flow.
- Delegated Microsoft Graph permissions `Files.Read.All` and `Sites.Read.All`.
- Retrieval API access for the calling user through a Microsoft 365 Copilot license or tenant-enabled pay-as-you-go consumption.

Direct retrieval does not require Azure OpenAI, another model deployment, Azure AI Search, or a separate content index.

For app registration instructions, see [Microsoft Entra ID setup](entra-id-setup.md). For the pay-as-you-go infrastructure requirements, see [Licensing and billing](#licensing-and-billing).

## 2. Install the package

```sh
dotnet add package Acterion.Agents.AI.Microsoft365.Retrieval
```

## 3. Supply delegated identity

Implement `IMicrosoft365RetrievalTokenProvider` in the host. It must return a delegated Microsoft Graph access token for the current operation:

```csharp
using Acterion.Agents.AI.Microsoft365.Retrieval;

public sealed class HostRetrievalTokenProvider : IMicrosoft365RetrievalTokenProvider
{
    public Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken = default)
    {
        // Use the host's identity library to acquire a delegated Graph token.
        throw new NotImplementedException();
    }
}
```

The token provider is intentionally the identity boundary. The package does not choose a credential, cache tokens, trigger consent, or fall back to application permissions.

Choose an implementation that matches the host:

| Host | Typical delegated flow | Reference |
| --- | --- | --- |
| Console or desktop application | Device code or interactive browser | [Console sample](../samples/Microsoft365Retrieval.Console/README.md) |
| Protected ASP.NET Core API | OAuth 2.0 On-Behalf-Of | [ASP.NET Core sample](../samples/Microsoft365Retrieval.AspNetCore/README.md) |
| Existing authenticated host | Its established delegated token acquisition | Implement the package interface as a thin adapter |

## 4. Register retrieval

Register the host token provider and the retrieval services:

```csharp
using Acterion.Agents.AI.Microsoft365.Retrieval;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();

services.AddSingleton<IMicrosoft365RetrievalTokenProvider, HostRetrievalTokenProvider>();
services.AddMicrosoft365Retrieval(options =>
{
    options.MaximumNumberOfResults = 8;
});
```

Without a filter, the API searches SharePoint content available to the signed-in user. To narrow the query to a trusted site:

```csharp
services.AddMicrosoft365Retrieval(options =>
{
    options.FilterExpression = SharePointRetrievalFilter
        .Path(new Uri("https://contoso.sharepoint.com/sites/engineering/"))
        .Expression;
});
```

A filter is query scope, not authorization. Microsoft 365 permission trimming remains the content-access boundary, and the host remains responsible for application authorization.

## Direct retrieval

Resolve `IMicrosoft365RetrievalClient` and call `RetrieveAsync`:

```csharp
await using ServiceProvider serviceProvider = services.BuildServiceProvider();

IMicrosoft365RetrievalClient retrievalClient =
    serviceProvider.GetRequiredService<IMicrosoft365RetrievalClient>();

IReadOnlyList<Microsoft365RetrievalHit> hits = await retrievalClient.RetrieveAsync(
    "What is our incident response process?",
    cancellationToken);

foreach (Microsoft365RetrievalHit hit in hits)
{
    Console.WriteLine(hit.WebUrl);

    foreach (Microsoft365RetrievalExtract extract in hit.Extracts)
    {
        Console.WriteLine(extract.Text);
    }
}
```

This is the simplest path for search interfaces, diagnostics, custom orchestration, and applications that do not use Agent Framework.

## Automatic agent retrieval

Automatic retrieval performs a search before each model invocation and contributes the results as context. Start with an existing `IChatClient` from your selected model provider:

```csharp
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

IChatClient retrievalChatClient = new ChatClientBuilder(chatClient)
    .UseMicrosoft365Retrieval(
        TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke)
    .Build(serviceProvider);

AIAgent agent = retrievalChatClient.AsAIAgent(
    new ChatClientAgentOptions(),
    services: serviceProvider);
```

The package is model-provider independent. The host configures and authenticates `chatClient`; those credentials are unrelated to the delegated Microsoft Graph token.

## On-demand agent retrieval

On-demand retrieval exposes search as a tool and lets the model decide whether and how to call it:

```csharp
TextSearchProviderOptions retrievalOptions = new()
{
    SearchTime = TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling,
};

IChatClient retrievalChatClient = new ChatClientBuilder(chatClient)
    .UseMicrosoft365Retrieval(retrievalOptions)
    .Build(serviceProvider);

AIAgent agent = retrievalChatClient.AsAIAgent(
    new ChatClientAgentOptions { UseProvidedChatClientAsIs = true },
    services: serviceProvider);
```

`UseProvidedChatClientAsIs = true` is required because the retrieval extension composes Agent Framework's native function invocation. It also disables the default `ChatClientAgent` decorators. Add any other middleware your host requires to `ChatClientBuilder` before creating the agent.

In on-demand mode, the model controls the search query. Treat that query and all retrieved content as untrusted input.

## Licensing and billing

The calling user needs one of these access paths:

- A Microsoft 365 Copilot add-on license.
- Retrieval API pay-as-you-go consumption enabled for the tenant.

Pay-as-you-go is currently preview and requires:

- An Azure subscription in good standing.
- An Azure resource group.
- Owner or Contributor access for setup.
- Microsoft 365 administrator access.
- At least one Microsoft 365 Copilot license in the tenant during enablement and use.

Enable it in the Microsoft 365 admin center under **Copilot > Billing & usage > Pay-as-you-go > Microsoft 365 Copilot Retrieval API**. Enablement can take approximately two hours to propagate. Review the current [Microsoft pricing and enablement documentation](https://learn.microsoft.com/microsoft-365/copilot/extensibility/api/ai-services/retrieval/paygo-retrieval) before using this route.

The package does not create Azure infrastructure or control billing. Azure infrastructure is unnecessary when every calling user already has the required Microsoft 365 Copilot license.

## Run a reference sample

The quickest end-to-end validation does not invoke a model:

```sh
cp samples/Microsoft365Retrieval.Console/appsettings.json \
    samples/Microsoft365Retrieval.Console/appsettings.local.json

dotnet run --project samples/Microsoft365Retrieval.Console -- --retrieval-only
```

Configure the tenant and public-client application first by following the [console sample guide](../samples/Microsoft365Retrieval.Console/README.md).

## Next steps

- Review every setting in the [configuration reference](configuration.md).
- Configure the correct host flow in [Microsoft Entra ID setup](entra-id-setup.md).
- Apply the [security and production guidance](security.md) before exposing retrieval to users.
- Use [troubleshooting](troubleshooting.md) when the first live request fails.