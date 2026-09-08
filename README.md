# Microsoft 365 Retrieval for .NET agents

Bring permission-trimmed SharePoint knowledge into .NET applications and Microsoft Agent Framework agents with one focused package.

`Acterion.Agents.AI.Microsoft365.Retrieval` calls the Microsoft 365 Copilot Retrieval API directly and adapts its results to Agent Framework's native `TextSearchProvider`. Use the retrieval client by itself, or add grounded context to an existing agent without provisioning a separate search index.

> This is a community project and is not an official Microsoft package.

## Why use it?

- **No duplicate index**: retrieve from SharePoint content already indexed by Microsoft 365.
- **Permission trimmed**: results are evaluated for the signed-in user by Microsoft 365 and SharePoint.
- **Host-owned identity**: choose device code, interactive browser, On-Behalf-Of, or another delegated flow in your application.
- **Two integration levels**: call `IMicrosoft365RetrievalClient` directly or compose retrieval into an Agent Framework pipeline.
- **Typed SharePoint filters**: filter by location, document properties, people, labels, and modification time without assembling KQL by hand.

## Supported today

| Capability | Support |
| --- | --- |
| Data source | SharePoint Online |
| Identity | Delegated work or school identity |
| Runtime | .NET 10 |
| Direct retrieval | `IMicrosoft365RetrievalClient` |
| Agent integration | Microsoft Agent Framework `TextSearchProvider` |
| Retrieval timing | Before every model call or on demand |

Application permissions, app-only retrieval, OneDrive retrieval, and Microsoft 365 Copilot connector retrieval are not exposed by this package.

## Prerequisites

You need:

1. A Microsoft Entra app registration with delegated Microsoft Graph permissions `Files.Read.All` and `Sites.Read.All`.
2. A host authentication flow that obtains a delegated Graph token for the current user.
3. Access to the Microsoft 365 Copilot Retrieval API through either:
   - a Microsoft 365 Copilot license for the calling user; or
   - Retrieval API pay-as-you-go consumption enabled for the tenant.

The delegated Graph permissions do not require admin consent by definition, but tenant consent policies can still require administrator approval. Pay-as-you-go is a preview feature and requires an Azure subscription, an Azure resource group, Microsoft 365 administrator access, and at least one Microsoft 365 Copilot license in the tenant. A model deployment is optional for direct retrieval and required only when your application also invokes a model.

## Install

```sh
dotnet add package Acterion.Agents.AI.Microsoft365.Retrieval
```

## Quick start

Your host supplies an `IMicrosoft365RetrievalTokenProvider`; the package never selects or acquires credentials for you.

```csharp
using Acterion.Agents.AI.Microsoft365.Retrieval;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();

services.AddSingleton<IMicrosoft365RetrievalTokenProvider, MyGraphTokenProvider>();
services.AddMicrosoft365Retrieval(options =>
{
	options.MaximumNumberOfResults = 8;
	options.FilterExpression = SharePointRetrievalFilter
		.Path(new Uri("https://contoso.sharepoint.com/sites/engineering/"))
		.Expression;
});

await using ServiceProvider serviceProvider = services.BuildServiceProvider();
IMicrosoft365RetrievalClient retrieval =
	serviceProvider.GetRequiredService<IMicrosoft365RetrievalClient>();

IReadOnlyList<Microsoft365RetrievalHit> hits = await retrieval.RetrieveAsync(
	"What is our incident response process?");
```

`MyGraphTokenProvider` implements one method and returns a delegated token for Microsoft Graph:

```csharp
public sealed class MyGraphTokenProvider : IMicrosoft365RetrievalTokenProvider
{
	public Task<string> GetAccessTokenAsync(
		CancellationToken cancellationToken = default)
	{
		// Acquire and return a delegated Microsoft Graph access token here.
		throw new NotImplementedException();
	}
}
```

See the runnable [console sample](https://github.com/luismanez/agent-framework-extensions/tree/main/samples/Microsoft365Retrieval.Console) for Azure Identity device-code authentication and retrieval without a model.

## Add retrieval to an agent

Register the same retrieval services, then decorate your model client before creating the agent:

```csharp
IChatClient retrievalChatClient = new ChatClientBuilder(chatClient)
	.UseMicrosoft365Retrieval(
		TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke)
	.Build(serviceProvider);

AIAgent agent = retrievalChatClient.AsAIAgent(
	new ChatClientAgentOptions(),
	services: serviceProvider);
```

Use `OnDemandFunctionCalling` when the model should decide whether to search. In that mode, create the agent with `UseProvidedChatClientAsIs = true`; this preserves the package's native function-invocation pipeline but disables Agent Framework's default agent decorators, so compose any other required decorators on `ChatClientBuilder` yourself.

The [ASP.NET Core sample](https://github.com/luismanez/agent-framework-extensions/tree/main/samples/Microsoft365Retrieval.AspNetCore) demonstrates a protected API using Microsoft Identity Web On-Behalf-Of authentication.

## Documentation

- [Getting started](https://github.com/luismanez/agent-framework-extensions/blob/main/docs/getting-started.md)
- [Microsoft Entra ID setup](https://github.com/luismanez/agent-framework-extensions/blob/main/docs/entra-id-setup.md)
- [Configuration reference](https://github.com/luismanez/agent-framework-extensions/blob/main/docs/configuration.md)
- [Security and production guidance](https://github.com/luismanez/agent-framework-extensions/blob/main/docs/security.md)
- [Troubleshooting](https://github.com/luismanez/agent-framework-extensions/blob/main/docs/troubleshooting.md)

## Security boundaries

- Retrieval is delegated-only. Never fall back to application credentials when user token acquisition fails.
- A filter narrows a query; it is not authorization. Build filters from trusted application configuration, not raw user input.
- Microsoft 365 permission trimming is the content-access boundary. Your host still owns endpoint, business, and tool authorization.
- Retrieved documents are untrusted input. Apply prompt-injection defenses appropriate to your application.
- Do not log access tokens, authorization headers, raw Graph responses, or retrieved document content by default.

## Samples

- [Console](https://github.com/luismanez/agent-framework-extensions/tree/main/samples/Microsoft365Retrieval.Console): device-code sign-in, persistent token cache, retrieval-only mode, and an Azure OpenAI agent flow.
- [ASP.NET Core](https://github.com/luismanez/agent-framework-extensions/tree/main/samples/Microsoft365Retrieval.AspNetCore): bearer authentication, OBO token acquisition, and grounded responses from a protected API.

## License

Licensed under the [MIT License](https://github.com/luismanez/agent-framework-extensions/blob/main/LICENSE).
