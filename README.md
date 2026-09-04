# agent-framework-extensions
Community-driven .NET extensions and integrations for Microsoft Agent Framework.

`Acterion.Agents.AI.Microsoft365.Retrieval` provides a lightweight, native Microsoft Agent Framework `TextSearchProvider` integration for calling the Microsoft 365 Copilot Retrieval API directly from .NET applications.

The package consumes a delegated Microsoft Graph token through `IMicrosoft365RetrievalTokenProvider`; it does not acquire identity and does not depend on Azure Identity, Microsoft Identity Web, MSAL, or ASP.NET Core. Host applications select their authentication flow and supply the provider. The planned console and ASP.NET Core samples demonstrate Azure Identity device-code and Microsoft Identity Web On-Behalf-Of flows without moving those SDKs into the base package.

Microsoft provides higher-level SharePoint grounding options through Foundry Agent Service and Foundry IQ. This package targets a different scenario: .NET developers who already use Microsoft Agent Framework and want to call the Microsoft 365 Copilot Retrieval API directly through a native `TextSearchProvider` integration, without introducing additional Foundry IQ or Azure AI Search infrastructure.

It is a community project and is not an official Microsoft package. See the [project specification](specs/SPEC/SPEC.md) for architecture, security guidance, and current platform comparison notes.
