# agent-framework-extensions
Community-driven .NET extensions and integrations for Microsoft Agent Framework.

`Acterion.Agents.AI.Microsoft365.Retrieval` provides a lightweight, native Microsoft Agent Framework `TextSearchProvider` integration for calling the Microsoft 365 Copilot Retrieval API directly from .NET applications.

The package consumes a delegated Microsoft Graph token through `IMicrosoft365RetrievalTokenProvider`; it does not acquire identity and does not depend on Azure Identity, Microsoft Identity Web, MSAL, or ASP.NET Core. Host applications select their authentication flow and supply the provider. The planned console and ASP.NET Core samples demonstrate Azure Identity device-code and Microsoft Identity Web On-Behalf-Of flows without moving those SDKs into the base package.

Microsoft provides higher-level SharePoint grounding options through Foundry Agent Service and Foundry IQ. This package targets a different scenario: .NET developers who already use Microsoft Agent Framework and want to call the Microsoft 365 Copilot Retrieval API directly through a native `TextSearchProvider` integration, without introducing additional Foundry IQ or Azure AI Search infrastructure.

It is a community project and is not an official Microsoft package. See the [project specification](specs/SPEC/SPEC.md) for architecture, security guidance, and current platform comparison notes.

## Typed SharePoint filters

Use `SharePointRetrievalFilter` to create the supported `Path` and `SiteID` constraints from trusted application configuration. Assign its `Expression` to the existing `FilterExpression` option:

```csharp
Uri engineeringSite = new("https://contoso.sharepoint.com/sites/engineering/");
Guid hrSiteId = Guid.Parse("f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6");

options.FilterExpression = SharePointRetrievalFilter.Path(engineeringSite).Expression;
options.FilterExpression = SharePointRetrievalFilter.SiteId(hrSiteId).Expression;
options.FilterExpression = SharePointRetrievalFilter
	.AnyOf(
		SharePointRetrievalFilter.Path(engineeringSite),
		SharePointRetrievalFilter.SiteId(hrSiteId))
	.Expression;
options.FilterExpression = SharePointRetrievalFilter
	.AnyOf(
		SharePointRetrievalFilter.Path(engineeringSite),
		SharePointRetrievalFilter.AnyOf(
			SharePointRetrievalFilter.SiteId(hrSiteId),
			SharePointRetrievalFilter.Path(new Uri("https://contoso.sharepoint.com/sites/legal/"))))
	.Expression;
```

`FilterExpression` remains available for advanced KQL scenarios outside this typed builder's narrow contract. Only construct typed filters from trusted application values, never arbitrary end-user input. A filter narrows retrieval; it is not authorization. Incorrectly applied or ignored filtering must never expose content the delegated user cannot already access. Microsoft 365 permission trimming remains the authoritative content-access boundary, and the host remains responsible for endpoint authorization and business rules.

## Agent Framework usage

Register the host-owned token provider and Retrieval services, then decorate the model client before creating the agent:

```csharp
IChatClient retrievalChatClient = new ChatClientBuilder(chatClient)
	.UseMicrosoft365Retrieval(
		TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke)
	.Build(serviceProvider);

AIAgent agent = retrievalChatClient.AsAIAgent(
	new ChatClientAgentOptions(),
	services: serviceProvider);
```

For on-demand retrieval, pass `OnDemandFunctionCalling` to `UseMicrosoft365Retrieval` and create the agent with `UseProvidedChatClientAsIs = true`. The extension then uses Agent Framework's native function invocation after `TextSearchProvider` adds its search tool; the model decides when to search.

```csharp
AIAgent agent = retrievalChatClient.AsAIAgent(
	new ChatClientAgentOptions { UseProvidedChatClientAsIs = true },
	services: serviceProvider);
```

`UseProvidedChatClientAsIs` disables all default `ChatClientAgent` decorators. Configure any other Agent Framework decorators required by the host on the `ChatClientBuilder` before creating the agent.
