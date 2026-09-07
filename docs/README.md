# Documentation

Use these guides to move from tenant setup to a production-ready Microsoft 365 Retrieval integration.

## Start here

1. [Getting started](getting-started.md): choose an integration mode, install the package, and make a first retrieval call.
2. [Microsoft Entra ID setup](entra-id-setup.md): configure delegated permissions for a desktop or web-hosted application.
3. [Configuration reference](configuration.md): understand every retrieval option, default, and validation rule.
4. [Security and production guidance](security.md): preserve delegated identity, authorization boundaries, and safe handling of retrieved content.
5. [Troubleshooting](troubleshooting.md): diagnose authentication, authorization, licensing, filtering, and Agent Framework issues.

## Choose your path

| Goal | Recommended path |
| --- | --- |
| Prove Retrieval works without a model | Run the [console sample](../samples/Microsoft365Retrieval.Console/README.md) with `--retrieval-only` |
| Retrieve SharePoint content from application code | Follow [direct retrieval](getting-started.md#direct-retrieval) |
| Ground an Agent Framework agent before every model call | Follow [automatic retrieval](getting-started.md#automatic-agent-retrieval) |
| Let the model decide when to search | Follow [on-demand retrieval](getting-started.md#on-demand-agent-retrieval) |
| Build a protected employee-facing API | Start with the [ASP.NET Core sample](../samples/Microsoft365Retrieval.AspNetCore/README.md) |

## What the package owns

The package owns:

- Retrieval API request construction and transport.
- Response parsing and typed retrieval results.
- SharePoint `Path` and `SiteID` filter construction.
- Agent Framework `TextSearchProvider` adaptation.

The host application owns:

- User authentication and delegated Microsoft Graph token acquisition.
- Endpoint, business, and tool authorization.
- Model selection, credentials, and deployment.
- Consent, Conditional Access, and claims-challenge user experiences.
- Logging, telemetry, content handling, and application guardrails.

The package deliberately has no dependency on Azure Identity, Microsoft Identity Web, MSAL, ASP.NET Core, or a model provider.

## Platform documentation

- [Microsoft 365 Copilot Retrieval API overview](https://learn.microsoft.com/microsoft-365-copilot/extensibility/api/ai-services/retrieval/overview)
- [Microsoft Graph permissions reference](https://learn.microsoft.com/graph/permissions-reference)
- [Retrieval API pay-as-you-go consumption](https://learn.microsoft.com/microsoft-365/copilot/extensibility/api/ai-services/retrieval/paygo-retrieval)
