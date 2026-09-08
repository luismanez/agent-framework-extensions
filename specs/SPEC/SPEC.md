# Agent Framework Extensions — Specification

**Repository:** `agent-framework-extensions`
**Solution:** `Acterion.Agents.AI.slnx`
**Initial NuGet package:** `Acterion.Agents.AI.Microsoft365.Retrieval`
**Initial target:** .NET 10 / Microsoft Agent Framework
**License:** MIT
**Status:** Initial implementation specification

---

## 1. Purpose

`agent-framework-extensions` is an open-source repository containing small, focused .NET extensions and integrations for Microsoft Agent Framework.

The repository is intentionally designed as a **multi-package monorepo**, but only packages that solve a real use case should be created. The initial release contains a single package:

```text
Acterion.Agents.AI.Microsoft365.Retrieval
```

This package integrates the **Microsoft 365 Copilot Retrieval API** with Microsoft Agent Framework so that .NET agents can retrieve permission-aware grounding content from Microsoft 365, initially SharePoint, without requiring developers to build and maintain their own ingestion, chunking, embedding, indexing, or ACL synchronization pipeline.

The package should feel like a natural extension of `Microsoft.Agents.AI`, reuse Agent Framework abstractions whenever possible, and avoid introducing another agent or RAG abstraction.

### 1.1 Specification hierarchy

This document is the global product and release specification. It defines the architecture, cross-cutting constraints, release acceptance criteria, and boundaries that apply to the complete v0.1 package.

Implementation is divided into five feature specifications under `specs/features/`:

1. [`001-sharepoint-retrieval-client.md`](../features/001-sharepoint-retrieval-client/001-sharepoint-retrieval-client.md) — validated SharePoint Retrieval API access;
2. [`002-agent-framework-integration.md`](../features/002-agent-framework-integration/002-agent-framework-integration.md) — mapping and integration with `TextSearchProvider`;
3. [`003-console-reference-sample.md`](../features/003-console-reference-sample/003-console-reference-sample.md) — delegated console authentication through sample-owned Azure Identity code;
4. [`004-aspnetcore-reference-sample.md`](../features/004-aspnetcore-reference-sample/004-aspnetcore-reference-sample.md) — delegated ASP.NET Core On-Behalf-Of authentication through sample-owned Microsoft Identity Web code;
5. [`005-typed-sharepoint-retrieval-filters.md`](../features/005-typed-sharepoint-retrieval-filters/005-typed-sharepoint-retrieval-filters.md) — typed construction of trusted SharePoint path and site-ID filters.

Each feature MUST pass its own specify, plan, tasks, and implementation gates before it is considered complete. Feature specifications refine this document but MUST NOT override it. If a conflict is found, update or clarify the global specification first, then align the affected feature specification.

CI, packaging, repository-wide documentation, security review, and final quality checks remain release-level concerns in this document. They are not separate product features.

---

## 2. High-Level Goals

The initial package MUST:

1. Make it easy for a .NET developer using Microsoft Agent Framework to ground an agent with content retrieved through the Microsoft 365 Copilot Retrieval API.
2. Reuse the Agent Framework `TextSearchProvider` model instead of implementing a parallel RAG lifecycle.
3. Consume delegated Microsoft Graph access tokens through a minimal host-provided abstraction without acquiring identity inside the package.
4. Preserve Microsoft 365 permission trimming by calling the Retrieval API using the current user's delegated identity.
5. Provide source URLs and useful source names so Agent Framework can generate citations.
6. Support both Agent Framework retrieval behaviors:
   - retrieval before the model invocation;
   - retrieval exposed as an on-demand function/tool.
7. Be small, understandable, testable, and production-oriented.
8. Keep package-level dependencies and public APIs minimal.
9. Prepare the repository for future `Acterion.Agents.AI.*` NuGet packages without creating speculative `Core`, `Common`, or `Abstractions` projects.
10. Expose useful Microsoft 365 Copilot Retrieval API controls directly in a normal .NET / Agent Framework application, including trusted KQL scoping, result limits, and requested metadata.

---

## 3. Non-Goals

The initial release MUST NOT attempt to implement:

- a generic RAG framework;
- a vector database;
- embeddings;
- content ingestion or indexing;
- SharePoint crawling;
- SharePoint ACL replication;
- custom document chunking;
- Microsoft Graph Search API integration;
- MCP server/client functionality;
- tool-level authorization;
- Entra group authorization;
- Conditional Access challenge handling;
- interactive consent flows inside an agent execution;
- token caching infrastructure;
- business authorization rules;
- custom memory infrastructure;
- identity acquisition or credential selection inside the Retrieval package;
- a base-package dependency on Azure Identity, Microsoft Identity Web, MSAL, or ASP.NET Core;
- a replacement for `Microsoft.Identity.Web`;
- a replacement for `TextSearchProvider`;
- OneDrive-specific features;
- Copilot Connector-specific features;
- thumbnail handling;
- Microsoft 365 Copilot licensing or billing management.
- Foundry IQ knowledge sources, knowledge bases, query planning, routing, reranking, answer synthesis, or managed multi-source retrieval.
- Azure AI Search services, SharePoint indexing pipelines, or indexed SharePoint knowledge sources.

The library should integrate existing Microsoft capabilities rather than reimplement them.

---

## 4. Naming and Branding

### 4.1 Repository

Repository name:

```text
agent-framework-extensions
```

Repository display title:

```text
Agent Framework Extensions
```

Suggested repository description:

> .NET extensions and integrations for Microsoft Agent Framework.

The repository name intentionally does not contain the Acterion brand. The repository describes the ecosystem/project; package names and namespaces identify the publisher.

### 4.2 Solution

```text
Acterion.Agents.AI.slnx
```

### 4.3 NuGet package

```text
Acterion.Agents.AI.Microsoft365.Retrieval
```

### 4.4 Root namespace

```text
Acterion.Agents.AI.Microsoft365.Retrieval
```

### 4.5 Future package naming

Future packages should follow:

```text
Acterion.Agents.AI.<CapabilityOrIntegration>
```

Examples of possible future names only:

```text
Acterion.Agents.AI.MicrosoftGraph
Acterion.Agents.AI.SharePoint
Acterion.Agents.AI.Entra
```

These projects MUST NOT be created until there is an actual feature that justifies them.

Do not create any of the following preemptively:

```text
Acterion.Agents.AI.Core
Acterion.Agents.AI.Common
Acterion.Agents.AI.Abstractions
```

Shared packages should only appear after real duplication or dependency boundaries justify them.

---

## 5. Repository Structure

Initial structure:

```text
agent-framework-extensions/
│
├── src/
│   └── Acterion.Agents.AI.Microsoft365.Retrieval/
│       ├── Acterion.Agents.AI.Microsoft365.Retrieval.csproj
│       ├── AgentFramework/
│       ├── Authentication/
│       └── Retrieval/
│           ├── Filtering/
│           └── Models/
│
├── tests/
│   └── Acterion.Agents.AI.Microsoft365.Retrieval.Tests/
│       ├── Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj
│       ├── AgentFramework/
│       ├── Authentication/
│       ├── PublicContract/
│       ├── Retrieval/
│       └── TestDoubles/
│
├── samples/
│   ├── Microsoft365Retrieval.Console/
│   │   ├── Microsoft365Retrieval.Console.csproj
│   │   └── ...
│   └── Microsoft365Retrieval.AspNetCore/
│       ├── Microsoft365Retrieval.AspNetCore.csproj
│       └── ...
│
├── .github/
│   └── workflows/
│       └── ci.yml
│
├── eng/
│   ├── Directory.Build.props
│   └── Directory.Packages.props
│
├── nuget/
│   └── nuget-package.props
│
├── Directory.Build.props
├── Directory.Packages.props
├── Acterion.Agents.AI.slnx
├── README.md
├── LICENSE
└── .gitignore
```

The structure MUST make adding another package later as simple as adding another project under `src/`, its tests under `tests/`, and optional samples under `samples/`.

Within a package, folders group code by capability and ownership boundary. Public namespaces remain package-oriented unless a separate consumer-facing namespace is intentionally introduced; physical folders do not require matching namespace segments.

---

## 6. Product Positioning

Suggested README introduction:

> **Agent Framework Extensions** provides community-driven .NET extensions and integrations for Microsoft Agent Framework.
>
> The first package, `Acterion.Agents.AI.Microsoft365.Retrieval`, connects Agent Framework agents to the Microsoft 365 Copilot Retrieval API, enabling permission-aware grounding over Microsoft 365 content such as SharePoint.

The project should explicitly state that it is a community project and is **not an official Microsoft package**.

### 6.1 Architectural niche and Microsoft platform context

`Acterion.Agents.AI.Microsoft365.Retrieval` is a lightweight, model-provider-independent integration between Microsoft Agent Framework and the Microsoft 365 Copilot Retrieval API. It is for .NET developers who already have an Agent Framework application and want Microsoft 365 grounding directly through a native `TextSearchProvider` integration.

> Native Microsoft 365 Retrieval for Microsoft Agent Framework, without Foundry IQ or Azure AI Search infrastructure.

The package does **not** require a Microsoft Foundry Project, a Foundry SharePoint Project Connection, Foundry Agent Service, Foundry IQ, a Knowledge Base, or an Azure AI Search service. Its model-provider independence is an architectural property of the `TextSearchProvider` integration, not a claim that Microsoft Foundry materially restricts model choice.

This package is not positioned as universally better than Microsoft Foundry offerings. Microsoft provides higher-level SharePoint grounding options through Foundry Agent Service and Foundry IQ. Those products address different needs and may be the appropriate choice when managed knowledge infrastructure, knowledge bases, multi-source retrieval, query planning, routing, or reranking are required.

The relevant architectures are:

```text
Acterion package
Microsoft Agent Framework
    -> TextSearchProvider
    -> Acterion.Agents.AI.Microsoft365.Retrieval
    -> Microsoft Graph /copilot/retrieval
    -> SharePoint

Foundry Agent Service SharePoint Tool
Agent
    -> Foundry SharePoint Tool
    -> Foundry Project Connection
    -> Microsoft 365 Copilot Retrieval API
    -> SharePoint

Foundry IQ Remote SharePoint
Agent / application
    -> Foundry IQ Knowledge Base
    -> Remote SharePoint Knowledge Source
    -> Microsoft 365 Copilot Retrieval API
    -> SharePoint
```

Foundry IQ has two distinct SharePoint approaches that MUST NOT be conflated:

- **Remote SharePoint** queries SharePoint through the Microsoft 365 Copilot Retrieval API at retrieval time. It does not ingest SharePoint content into an Azure AI Search index, retains Microsoft 365 permission trimming, and can use Retrieval API filtering and metadata configuration. It still requires Foundry IQ/Azure AI Search infrastructure, a knowledge source, and normally a knowledge base.
- **Indexed SharePoint** ingests SharePoint content through an Azure AI Search indexing pipeline. It is a traditional indexed RAG architecture with configuration for SharePoint indexing scope and content processing, and requires managing that search/indexing pipeline.

Multi-site SharePoint retrieval is therefore **not** unique to this package across Microsoft's platform: Foundry IQ Remote SharePoint can use Retrieval API filtering for multi-site scenarios. The direct integration remains useful because it exposes Retrieval API controls inside the existing .NET / Agent Framework application without adding a Knowledge Base or Azure AI Search resource.

### 6.2 Comparison guidance

This comparison describes the currently documented surfaces, not undocumented product limits. Foundry SharePoint and Foundry IQ features are evolving, and preview behavior MUST be rechecked against current Microsoft Learn documentation before release documentation makes more specific claims.

| Capability | Acterion package | Foundry Agent Service SharePoint Tool | Foundry IQ Remote SharePoint |
| --- | --- | --- | --- |
| Copilot Retrieval API | Direct | Indirect | Indirect |
| Delegated / permission-trimmed retrieval | Yes | Yes | Yes |
| Multiple SharePoint sites | Yes, through KQL scopes | Limited in the documented site/folder connection model | Yes, through Retrieval API filtering |
| KQL / `filterExpression` | Yes | Not fully exposed in the documented tool configuration | Yes |
| Resource metadata control | Yes | Limited documented abstraction | Yes |
| Requires Foundry Project | No | Yes | Foundry / Azure AI Search infrastructure |
| Requires SharePoint Project Connection | No | Yes | Different Knowledge Source model |
| Requires Azure AI Search service | No | No | Yes |
| Requires Knowledge Source / Knowledge Base | No | No | Yes |
| `BeforeAIInvoke` `TextSearchProvider` | Yes | Tool-oriented, not this native provider abstraction | Not the same abstraction |
| On-demand search | Yes | Yes | Yes, through the knowledge base/query model |
| Managed multi-source knowledge platform | No | No | Yes |
| Lightweight direct Agent Framework integration | Yes | No | No |

The Foundry Agent Service SharePoint Tool can be configured and provisioned through SDKs, REST, ARM/Bicep, and other IaC workflows. Do not describe this package as "code-first" in contrast to a portal-only Foundry alternative. The meaningful distinction is: **no Foundry Agent Service infrastructure required**.

---

## 7. Initial Package: Functional Overview

The initial package bridges:

```text
Microsoft Agent Framework
        │
        │ TextSearchProvider
        ▼
Acterion.Agents.AI.Microsoft365.Retrieval
        │
        │ Microsoft Graph
        ▼
Microsoft 365 Copilot Retrieval API
        │
        │ delegated user identity
        ▼
SharePoint
```

The package should convert Microsoft 365 Retrieval API results into:

```csharp
TextSearchProvider.TextSearchResult
```

and let Agent Framework own:

- when retrieval occurs;
- whether retrieval is automatic or tool-driven;
- conversation-aware search input;
- injecting retrieved content into model context;
- formatting the context;
- citation prompting.

---

## 8. Microsoft 365 Copilot Retrieval API

### 8.1 Endpoint

Use the v1.0 endpoint:

```http
POST https://graph.microsoft.com/v1.0/copilot/retrieval
```

Do not use the beta endpoint in the default implementation.

### 8.2 Initial data source

The MVP supports:

```json
{
  "dataSource": "sharePoint"
}
```

The internal design should avoid preventing future support for:

```text
oneDriveBusiness
externalItem
```

but these data sources are not required for v0.1.

Do not expose enum values or public options that appear supported if the package does not actually support and test them.

### 8.3 Request options

The package MUST support:

- `queryString`;
- `filterExpression`;
- `resourceMetadata`;
- `maximumNumberOfResults`.

Initial defaults:

```text
Data source: SharePoint
Maximum results: 8
Resource metadata:
  - title
  - author
```

The exact default result count may be adjusted during implementation if testing shows a better default, but it MUST remain conservative to avoid unnecessary context growth.

### 8.4 API constraints

The implementation MUST validate known client-side constraints before sending the request:

- `queryString` must not be null, empty, or whitespace;
- `queryString` must not exceed 1,500 characters;
- `maximumNumberOfResults` must be between 1 and 25.

Fail fast with clear argument/configuration exceptions for invalid local input.

### 8.5 Filter expression

`filterExpression` uses Microsoft 365/SharePoint KQL.

Example:

```text
path:"https://contoso.sharepoint.com/sites/Engineering/"
```

Example with file types:

```text
path:"https://contoso.sharepoint.com/sites/Engineering/"
AND (FileType:"pdf" OR FileType:"docx" OR FileType:"pptx")
```

Important security rule:

> `filterExpression` is a retrieval scope, not an authorization boundary.

The Microsoft 365 Retrieval API can execute an incorrectly formed KQL filter without applying the intended scope. Therefore:

- never generate security assumptions from `filterExpression`;
- do not describe it as authorization;
- treat configured filters as trusted application configuration;
- do not concatenate arbitrary user input directly into KQL;
- document that the authoritative access boundary remains Microsoft 365 permission trimming for the delegated user.

Feature 005 introduced optional typed helpers for trusted SharePoint path and site-ID filters. The helper now covers every SharePoint property supported by the Retrieval API, inclusive last-modified bounds, and explicit `AND`, `OR`, and `NOT` composition. The raw `FilterExpression` option remains available for advanced KQL scenarios.

---

## 9. Response Mapping

The Microsoft 365 Retrieval API returns `retrievalHits`.

Each hit can contain:

- `webUrl`;
- multiple `extracts`;
- `resourceType`;
- `resourceMetadata`;
- sensitivity label information;
- other future fields.

### 9.1 Mapping to Agent Framework

For the MVP, map **one retrieval hit to one `TextSearchResult`**.

Suggested mapping:

```text
TextSearchResult.SourceName
    <- resourceMetadata["title"]
    <- fallback: filename/last URI segment
    <- fallback: webUrl

TextSearchResult.SourceLink
    <- webUrl

TextSearchResult.Text
    <- non-empty extracts concatenated in response order

TextSearchResult.RawRepresentation
    <- typed/raw retrieval hit model
```

When multiple extracts exist for a hit, concatenate them with a clear separator such as a newline.

Preserve the received hit sequence, but do not claim stable ranking or ordering across hits: the Retrieval API does not guarantee it. The adapter MUST NOT sort or apply its own relevance filtering in v0.1.

Do not include sensitivity label metadata in the LLM-visible text by default.

The raw response representation may retain metadata for debugging/custom formatting, but sensitive data MUST NOT be logged by default.

### 9.2 Empty results

If the Retrieval API returns zero hits, return an empty result sequence.

This is a valid result and must not be treated as an exception.

---

## 10. Agent Framework Integration

### 10.1 Primary integration model

The package SHOULD reuse:

```csharp
Microsoft.Agents.AI.TextSearchProvider
```

rather than implementing a new RAG lifecycle.

The package's essential adapter should expose a search function compatible with:

```csharp
Func<
    string,
    CancellationToken,
    Task<IEnumerable<TextSearchProvider.TextSearchResult>>>
```

A minimal usage path should be possible:

```csharp
var retrieval = serviceProvider
    .GetRequiredService<Microsoft365RetrievalSearch>();

var textSearchProvider = new TextSearchProvider(
    retrieval.SearchAsync,
    new TextSearchProviderOptions
    {
        SearchTime =
            TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
        RecentMessageMemoryLimit = 3
    });
```

Then:

```csharp
AIAgent agent = chatClient.AsAIAgent(
    new ChatClientAgentOptions
    {
        ChatOptions = new()
        {
            Instructions = """
                You are an internal company assistant.
                Answer questions using company knowledge when relevant.
                Cite source documents when available.
                """
        },
        AIContextProviders = [textSearchProvider]
    });
```

### 10.2 Convenience API

The package MUST provide a thin `ChatClientBuilder` extension that attaches Microsoft 365 retrieval without requiring the developer to resolve `Microsoft365RetrievalSearch` or construct `TextSearchProvider` manually.

The convenience API MUST expose the retrieval timing explicitly in the short overload and MUST accept the standard `TextSearchProviderOptions` type for advanced configuration:

```csharp
public static ChatClientBuilder UseMicrosoft365Retrieval(
    this ChatClientBuilder builder,
    TextSearchProviderOptions.TextSearchBehavior behavior);

public static ChatClientBuilder UseMicrosoft365Retrieval(
    this ChatClientBuilder builder,
    TextSearchProviderOptions options);
```

The package MUST NOT provide a parameterless overload because `BeforeAIInvoke` and `OnDemandFunctionCalling` have materially different execution semantics.

Service registration remains separate from per-agent configuration. Build the decorated chat client before creating the agent:

```csharp
IChatClient retrievalChatClient = new ChatClientBuilder(chatClient)
    .UseMicrosoft365Retrieval(
        TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke)
    .Build(serviceProvider);

AIAgent agent = retrievalChatClient.AsAIAgent(
    new ChatClientAgentOptions
    {
        ChatOptions = new()
        {
            Instructions = "You are an internal company assistant."
        }
    },
    services: serviceProvider);
```

Advanced Agent Framework options remain available without a package-specific configuration model:

```csharp
.UseMicrosoft365Retrieval(
    new TextSearchProviderOptions
    {
        SearchTime = TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling,
        RecentMessageMemoryLimit = 3,
        FunctionToolName = "search_company_knowledge"
    })
```

For `OnDemandFunctionCalling`, create the agent with the pre-decorated pipeline as is:

```csharp
AIAgent agent = retrievalChatClient.AsAIAgent(
    new ChatClientAgentOptions
    {
        UseProvidedChatClientAsIs = true,
    },
    services: serviceProvider);
```

The extension MUST:

- resolve `Microsoft365RetrievalSearch` from the `IServiceProvider` supplied to `ChatClientBuilder.Build`;
- create one `TextSearchProvider` for each built chat-client pipeline;
- attach it through `ChatClientBuilder.UseAIContextProviders`, which accepts the complete `AIContextProvider` contract;
- append Agent Framework's native `UseFunctionInvocation` decorator after the context provider only for `OnDemandFunctionCalling`;
- preserve other chat-client pipeline stages;
- pass the supplied `TextSearchProviderOptions` to `TextSearchProvider` without replacing its prompts, formatter, filters, memory settings, telemetry settings, or tool metadata;
- fail clearly during `Build` when `AddMicrosoft365Retrieval` has not registered the required services.

The caller creates the `ChatClientAgent` with `AsAIAgent`. For `OnDemandFunctionCalling`, it MUST set `UseProvidedChatClientAsIs = true` because the extension already contains the correctly ordered native function invoker; adding the agent's default outer invoker would handle tool calls before the provider-added tool is visible. This setting disables all default `ChatClientAgent` decorators, so the host MUST explicitly add to `ChatClientBuilder` any other decorators it requires. This enables on-demand retrieval without a custom search tool. The extension MUST NOT create or own the underlying agent, introduce a custom agent abstraction, or duplicate Agent Framework's context-provider lifecycle. `Microsoft365RetrievalProvider` MUST NOT be added.

Do not invent a large custom builder DSL for v0.1.

### 10.3 Retrieval behaviors

The integration MUST be usable with both Agent Framework modes:

#### Automatic — `BeforeAIInvoke`

```csharp
SearchTime =
    TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke;
```

Flow:

```text
User prompt
   ↓
M365 Retrieval
   ↓
Retrieved context
   ↓
Model
```

#### On demand — `OnDemandFunctionCalling`

```csharp
SearchTime =
    TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling;
```

When using the convenience extension, create the agent with `UseProvidedChatClientAsIs = true`. The extension adds Agent Framework's native function invoker inside the provider-enriched chat-client pipeline.

Flow:

```text
User prompt
   ↓
Model
   ↓
Model decides retrieval is required
   ↓
Search tool
   ↓
M365 Retrieval
   ↓
Model continues
```

The README/sample MUST clearly show how Agent Framework requires the provider to be attached for the selected behavior based on the current Agent Framework API.

Do not duplicate Agent Framework's search-tool implementation.

---

## 11. Public API Design

Keep the public API intentionally small.

Recommended initial public types:

```text
Microsoft365RetrievalOptions
IMicrosoft365RetrievalClient
Microsoft365RetrievalClient
Microsoft365RetrievalSearch
IMicrosoft365RetrievalTokenProvider
Microsoft365RetrievalChatClientBuilderExtensions
```

Identity-SDK-specific token providers MUST NOT be part of the base package public API. Hosts or optional future integration packages may implement `IMicrosoft365RetrievalTokenProvider`.

### 11.1 `Microsoft365RetrievalOptions`

Expected properties:

```csharp
public sealed class Microsoft365RetrievalOptions
{
    public int MaximumNumberOfResults { get; set; } = 8;

    public string? FilterExpression { get; set; }

    public IReadOnlyCollection<string> ResourceMetadata { get; set; }
        = ["title", "author"];
}
```

Implementation may use init-only properties or options validation as appropriate.

### 11.2 Retrieval client

The client owns:

- HTTP request construction;
- serialization;
- response deserialization;
- API-level error handling;
- cancellation;
- mapping API DTOs.

It MUST NOT own:

- web authentication;
- endpoint authorization;
- token cache persistence;
- user consent UI.

### 11.3 Search adapter

`Microsoft365RetrievalSearch` adapts the retrieval client result into `TextSearchProvider.TextSearchResult`.

This should contain very little Agent Framework-specific logic beyond the mapping.

---

## 12. Delegated Token Boundary

### 12.1 Required authentication model

The Microsoft 365 Retrieval API requires a **delegated work/school user identity** for the SharePoint scenario.

Application permissions are not supported for this API.

The package MUST therefore be designed around delegated user tokens supplied by the host.

The package consumes tokens; it does not acquire identity.

### 12.2 Required Graph permissions for SharePoint

The host application must acquire delegated Microsoft Graph permissions:

```text
Files.Read.All
Sites.Read.All
```

Both are required for SharePoint retrieval.

The package README MUST explicitly document the privilege implications of these permissions.

### 12.3 Token provider abstraction

The retrieval client MUST NOT be coupled to `HttpContext`, `TokenCredential`, `ITokenAcquisition`, `IConfidentialClientApplication`, or another identity-SDK type.

Use the package-owned minimal boundary:

```csharp
public interface IMicrosoft365RetrievalTokenProvider
{
    Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken = default);
}
```

The host MUST register an implementation that returns a delegated Microsoft Graph token for the current user and operation. The host chooses the acquisition mechanism, which may include:

- Azure Identity `DeviceCodeCredential` or `InteractiveBrowserCredential` in a native application;
- Microsoft Identity Web in an ASP.NET Core On-Behalf-Of flow;
- Azure Identity `OnBehalfOfCredential`;
- MSAL or an organization-specific token broker.

This keeps the retrieval client:

- testable;
- host-independent;
- reusable outside a Minimal API;
- easy to adapt to future hosting models.

The interface remains in the base package. Do not create a speculative authentication NuGet package. A reusable SDK-specific adapter may be proposed as a separate optional package only after multiple real hosts demonstrate the same stable integration contract.

### 12.4 Reference authentication flows

Feature 003 demonstrates console device-code authentication through sample-owned Azure Identity code. Feature 004 demonstrates ASP.NET Core On-Behalf-Of through sample-owned Microsoft Identity Web code.

Neither identity SDK is a transitive dependency of the base Retrieval package. `DefaultAzureCredential`, workload identity, and managed identity MUST NOT be presented as Retrieval API credentials because they normally represent application identity rather than the required delegated user identity.

The ASP.NET Core sample may use an in-memory token cache for simplicity. Its documentation MUST state that multi-instance production applications should use an appropriate distributed token cache.

### 12.5 Authentication failures

The package MUST NOT implement interactive login, consent UI, Conditional Access UX, or claims-challenge orchestration.

If delegated token acquisition fails:

- propagate or wrap the error with useful context;
- never expose access tokens;
- let the host application decide how authentication/consent failures are presented to the caller.

The package should not attempt to start a browser or interactive flow.

---

## 13. Authorization and Security Model

The security model is intentionally simple:

```text
Host API authentication
        +
Delegated user token
        +
Microsoft 365 permission trimming
```

### 13.1 Authoritative access control

The package MUST rely on Microsoft 365/SharePoint access control for content authorization.

If User A cannot access a document in Microsoft 365, the integration must not attempt to bypass or emulate that permission.

The library MUST NOT:

- use an application token as a fallback;
- switch to a more privileged identity on retrieval failure;
- implement custom ACL expansion;
- cache results globally without user isolation.

### 13.2 Cross-user data isolation

If caching is introduced in the future, cache keys MUST include a stable user/tenant isolation dimension.

The MVP SHOULD avoid result caching entirely unless there is a compelling reason.

### 13.3 Prompt injection

Retrieved SharePoint content is untrusted model input.

The README MUST explain that:

- retrieved documents can contain indirect prompt injection;
- retrieval results should be treated as data/context, not trusted instructions;
- application-level guardrails may still be required.

Do not place retrieved content into a system instruction unless Agent Framework itself explicitly handles it that way.

### 13.4 Logging

Default logs MUST NOT contain:

- access tokens;
- authorization headers;
- full retrieved document contents;
- full user queries at Information level;
- raw HTTP response bodies containing content.

Useful logs may include:

```text
Retrieval request started
Retrieval request completed
Result count
Elapsed duration
HTTP status
Data source
Retry/throttling information
```

Detailed query/result logging, if ever added, must require explicit opt-in.

---

## 14. HTTP and Resilience

Use `HttpClient` through standard .NET dependency injection / `IHttpClientFactory`.

Do not manually create a new `HttpClient` per request.

### 14.1 Status handling

Handle relevant failures explicitly:

- `400`: invalid retrieval request;
- `401`: invalid/missing delegated Graph token;
- `403`: missing Graph permission, licensing, consent, or inaccessible operation;
- `429`: throttling;
- `5xx`: Graph/Microsoft 365 service failure.

Exceptions should contain actionable context without leaking sensitive response data.

### 14.2 Retry policy

Do not build a custom retry framework.

If retries are implemented, use standard .NET HTTP resilience mechanisms and respect `Retry-After`.

Avoid retrying:

```text
400
401
403
```

by default.

### 14.3 Cancellation

All public async APIs MUST accept and honor `CancellationToken`.

---

## 15. Dependency Injection

Provide a clean service registration method:

```csharp
builder.Services.AddMicrosoft365Retrieval(options =>
{
    options.MaximumNumberOfResults = 8;
    options.FilterExpression =
        "path:\"https://contoso.sharepoint.com/sites/Engineering/\"";
});
```

The implementation should register:

- configured options;
- typed/named `HttpClient`;
- retrieval client;
- search adapter.

The host MUST register `IMicrosoft365RetrievalTokenProvider`. Base-package registration MUST NOT select a credential, configure authentication, or silently provide an application identity.

Avoid surprising global service registrations.

Do not alter the host application's authentication or authorization configuration automatically.

---

## 16. Reference Samples

Create two small hosts that prove the same package boundary through different delegated authentication flows:

```text
samples/Microsoft365Retrieval.Console
samples/Microsoft365Retrieval.AspNetCore
```

Both samples should be intentionally small and production-inspired. Identity adapters remain inside their respective sample projects.

### 16.1 Console scenario

The console sample authenticates a work or school user with Azure Identity device-code flow, adapts its `TokenCredential` through a sample-local `IMicrosoft365RetrievalTokenProvider`, and runs an Agent Framework agent with automatic retrieval.

It MUST NOT use `DefaultAzureCredential`, managed identity, or application credentials for Microsoft 365 Retrieval. Interactive browser authentication may be documented as an explicit alternative.

### 16.2 ASP.NET Core scenario

An authenticated employee calls:

```http
POST /api/assistant
```

with:

```json
{
  "message": "What is our remote work policy?"
}
```

The API:

1. validates the caller through Microsoft Entra ID;
2. runs a Microsoft Agent Framework agent;
3. retrieves SharePoint context through Microsoft 365 Retrieval using the user's delegated identity;
4. returns the agent response with source-aware grounding.

The v0.1 sample response contains an `answer` string. Citations produced through Agent Framework remain embedded in that answer; the sample does not expose a separate structured citation collection or reconstruct citations by rerunning retrieval.

The ASP.NET Core host owns Microsoft Identity Web configuration, On-Behalf-Of token acquisition, token caching, and its sample-local implementation of `IMicrosoft365RetrievalTokenProvider`.

### 16.3 Shared sample requirements

The samples collectively MUST demonstrate:

- console device-code authentication with Azure Identity;
- ASP.NET Core;
- Entra authentication;
- `Microsoft.Identity.Web`;
- OBO/delegated Graph token acquisition;
- Agent Framework;
- `TextSearchProvider`;
- `Acterion.Agents.AI.Microsoft365.Retrieval`;
- optional SharePoint site scoping using `filterExpression`;
- configuration through `appsettings.json` / user secrets / environment variables;
- no secrets committed to Git.

Automated builds and tests MUST NOT require a tenant, user login, or live service connection.

### 16.4 Model provider

The library itself MUST remain independent of the model provider.

The sample may use Azure OpenAI / Microsoft Foundry for convenience, but package code must not require it.

Prefer Entra-based authentication such as `DefaultAzureCredential`/managed identity for the model service instead of API keys where practical.

---

## 17. Configuration Examples

Console host environment variables:

```text
AZURE_TENANT_ID
AZURE_CLIENT_ID
MICROSOFT365_RETRIEVAL_FILTER
```

ASP.NET Core example:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<tenant-id>",
    "ClientId": "<client-id>",
    "ClientCredentials": [
      {
        "SourceType": "ClientSecret",
        "ClientSecret": "<use-user-secrets-or-environment-variable>"
      }
    ]
  },
  "Microsoft365Retrieval": {
    "MaximumNumberOfResults": 8,
    "FilterExpression": "path:\"https://contoso.sharepoint.com/sites/Engineering/\"",
    "ResourceMetadata": [
      "title",
      "author"
    ]
  }
}
```

Do not recommend storing a production client secret in source-controlled configuration.

For an ASP.NET Core confidential client, production guidance should prefer certificates, federated credentials where supported, or another deployment-specific secure credential mechanism supported by Microsoft Identity Web. These host credentials establish the OBO client; they are not an application-permission fallback for Retrieval.

---

## 18. Package Dependencies

Keep dependencies minimal.

Expected direct dependencies may include:

```text
Microsoft.Agents.AI
Microsoft.Extensions.Http
Microsoft.Extensions.Options.ConfigurationExtensions
```

Only add a dependency if package code directly requires it.

Sample projects may directly reference host-specific identity SDKs such as `Azure.Identity` or `Microsoft.Identity.Web`. Those references MUST NOT become transitive dependencies of `Acterion.Agents.AI.Microsoft365.Retrieval`.

Do not reference the Microsoft Graph SDK merely to call one Retrieval API endpoint unless it provides a clear implementation or maintenance advantage.

For the MVP, a small typed `HttpClient` over the stable v1.0 REST endpoint is preferred.

Centralize package versions using:

```text
Directory.Packages.props
```

---

## 19. Target Frameworks

The initial package, tests, and samples target:

```text
net10.0
```

The initial implementation should prefer a **single practical target framework** over unnecessary multi-targeting.

Do not target an out-of-support runtime merely for historical compatibility.

The initial dependency baseline, verified when the repository scaffold was created, is:

```text
Microsoft.Agents.AI 1.19.0
```

Sample-only dependency versions, including Azure Identity and Microsoft Identity Web, remain centrally managed when their sample projects reference them.

Package versions MUST remain centrally managed in `Directory.Packages.props`. Before implementing a feature, verify that these are still the latest stable compatible versions. A version update is allowed when it preserves the architectural intent and the complete solution remains buildable and testable.

Do not use preview package versions by default.

### 19.1 Canonical commands

The canonical repository verification commands are:

```powershell
dotnet restore Acterion.Agents.AI.slnx
dotnet build Acterion.Agents.AI.slnx --configuration Release --no-restore
dotnet test --solution Acterion.Agents.AI.slnx --configuration Release --no-build
dotnet pack src/Acterion.Agents.AI.Microsoft365.Retrieval/Acterion.Agents.AI.Microsoft365.Retrieval.csproj --configuration Release --no-build --output artifacts/packages
dotnet run --project samples/Microsoft365Retrieval.Console/Microsoft365Retrieval.Console.csproj
dotnet run --project samples/Microsoft365Retrieval.AspNetCore/Microsoft365Retrieval.AspNetCore.csproj
```

The test command uses Microsoft Testing Platform selected in `global.json`. Until the first test is added, Microsoft Testing Platform exits with code `8` for zero discovered tests; feature implementation MUST replace that temporary scaffold state with executable tests.

---

## 20. NuGet Packaging

Common NuGet metadata should live in:

```text
nuget/nuget-package.props
```

Each packable project imports this file explicitly. Repository-wide build settings and centrally managed package versions live under `eng/`, with the root `Directory.Build.props` and `Directory.Packages.props` retained as MSBuild entry points.

Suggested shared metadata:

```xml
<Project>
  <PropertyGroup>
    <Authors>Acterion</Authors>
    <Company>Acterion</Company>

    <PackageLicenseExpression>MIT</PackageLicenseExpression>

    <RepositoryType>git</RepositoryType>
    <RepositoryUrl>https://github.com/&lt;owner&gt;/agent-framework-extensions</RepositoryUrl>

    <PackageProjectUrl>https://github.com/&lt;owner&gt;/agent-framework-extensions</PackageProjectUrl>

    <PackageReadmeFile>README.md</PackageReadmeFile>
    <PackageIcon>acterion-nuget-icon-blue.png</PackageIcon>

    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

The actual GitHub owner and branding asset paths should be filled in from the repository.

Package description:

> Microsoft 365 Copilot Retrieval API integration for Microsoft Agent Framework on .NET.

Suggested NuGet tags:

```text
agent-framework
microsoft-agent-framework
microsoft365
sharepoint
microsoft-graph
copilot
rag
ai
dotnet
```

Include:

- package icon;
- package README;
- repository metadata;
- Source Link;
- symbols package if convenient.

---

## 21. Versioning

Initial package version:

```text
0.1.0
```

Use Semantic Versioning.

During `0.x`:

- API changes are allowed when justified;
- avoid needless churn;
- document breaking changes clearly.

Do not introduce a complex versioning system for the first release.

A simple centrally managed version is sufficient.

---

## 22. CI/CD

Create a GitHub Actions workflow for pull requests and pushes to the main branch.

Minimum CI:

```text
restore
build
test
pack
```

Requirements:

- Release configuration;
- warnings should be visible;
- tests must fail the workflow;
- package creation must be validated;
- no live tenant credentials required.

NuGet publishing may initially be manual.

If automatic publishing is added:

- trigger only from explicit release/tag workflow;
- use repository secrets or trusted publishing;
- never place NuGet API keys in source.

---

## 23. Testing Strategy

### 23.1 Unit tests

Tests MUST cover:

#### Request validation

- empty query;
- query over 1,500 characters;
- maximum result count below 1;
- maximum result count above 25.

#### Request serialization

Verify:

- `queryString`;
- `dataSource = sharePoint`;
- `filterExpression`;
- `resourceMetadata`;
- `maximumNumberOfResults`.

#### Response mapping

Verify:

- `webUrl -> SourceLink`;
- metadata title -> `SourceName`;
- title fallback behavior;
- multiple extracts are concatenated;
- empty extracts;
- empty hit list;
- unknown JSON fields do not break deserialization.

#### HTTP behavior

Verify:

- bearer token is sent;
- content type is JSON;
- cancellation propagates;
- non-success status handling;
- throttling metadata can be observed where relevant.

#### Security

Verify that:

- authorization headers are never included in exception messages;
- access tokens are never logged;
- raw document content is not logged by default.

### 23.2 Integration tests

Live Microsoft 365 integration tests are optional and MUST NOT be required for normal CI.

If added, mark them clearly and require explicit environment variables/secrets.

### 23.3 Sample validation

Both samples must compile as part of CI without acquiring credentials or contacting external services.

---

## 24. Error Model

Prefer a small, comprehensible exception model.

Do not create an exception class for every HTTP status.

A possible package exception:

```csharp
public sealed class Microsoft365RetrievalException : Exception
{
    public HttpStatusCode? StatusCode { get; }

    public string? RequestId { get; }
}
```

Exact design may vary.

Requirements:

- preserve inner exception when appropriate;
- include Graph request/correlation identifier if available;
- never include tokens;
- never include raw retrieved content;
- keep messages useful to a developer.

---

## 25. Observability

Use `ILogger<T>`.

Suggested event categories:

```text
RetrievalStarted
RetrievalCompleted
RetrievalFailed
RetrievalThrottled
TokenAcquisitionFailed
```

Useful dimensions:

```text
ResultCount
ElapsedMilliseconds
StatusCode
MaximumNumberOfResults
HasFilterExpression
```

Do not log the actual filter expression at Information level if it may contain tenant-specific URLs or sensitive metadata.

OpenTelemetry-specific instrumentation is not required for v0.1, but the logging structure should not prevent adding Activities/Metrics later.

---

## 26. Documentation Requirements

The root README MUST contain:

1. what the repository is;
2. status/disclaimer;
3. package table;
4. quick start;
5. prerequisites;
6. Entra app registration requirements;
7. delegated Graph permissions;
8. console and ASP.NET Core sample configuration;
9. automatic retrieval example;
10. on-demand retrieval example;
11. SharePoint site scoping example;
12. security considerations;
13. Microsoft 365 licensing prerequisite note;
14. links to both samples;
15. contributing information.

The package README may reuse relevant root documentation but should be directly useful when viewed on NuGet.org.

---

## 27. Security Documentation

Create a prominent README section named:

```text
Security considerations
```

It MUST state:

1. Retrieval runs using delegated user permissions.
2. `Files.Read.All` and `Sites.Read.All` are broad delegated permissions and require appropriate tenant review/consent.
3. Microsoft 365 performs permission trimming for the current user.
4. `filterExpression` must not be treated as an authorization boundary.
5. Retrieved SharePoint content is untrusted LLM context and can contain indirect prompt injection.
6. Tokens and retrieved content must not be logged by default.
7. Application authentication and endpoint authorization remain the responsibility of the host application.
8. The package does not implement fallback application permissions.
9. The package does not perform interactive consent or Conditional Access challenges.

---

## 28. YAGNI Rules

Codex should actively avoid overengineering.

Do not add:

- repository pattern;
- mediator;
- CQRS;
- custom result monad;
- generic provider framework;
- generic Graph API client;
- custom resilience framework;
- custom authorization framework;
- unnecessary factories;
- speculative abstraction layers;
- separate projects for three or four shared classes;
- source generators;
- reflection-heavy registration;
- dynamic plugin systems.

Prefer straightforward .NET code.

The package should be easy to understand by opening fewer than ten core source files.

---

## 29. Suggested Initial Internal Layout

This is guidance, not a rigid requirement:

```text
src/Acterion.Agents.AI.Microsoft365.Retrieval/
│
├── AgentFramework/
│   ├── Microsoft365RetrievalChatClientBuilderExtensions.cs
│   └── Microsoft365RetrievalSearch.cs
├── Authentication/
│   └── IMicrosoft365RetrievalTokenProvider.cs
└── Retrieval/
    ├── Filtering/
    ├── Models/
    ├── IMicrosoft365RetrievalClient.cs
    ├── Microsoft365RetrievalClient.cs
    ├── Microsoft365RetrievalException.cs
    └── Microsoft365RetrievalOptions.cs
```

If fewer folders make the package easier to navigate, prefer fewer folders.

Identity SDK adapters belong under their sample host, not under this project.

---

## 30. Expected Developer Experience

The final README should make the package boundary feel approximately this simple:

```csharp
builder.Services.AddScoped<
    IMicrosoft365RetrievalTokenProvider,
    HostDelegatedTokenProvider>();

builder.Services.AddMicrosoft365Retrieval(options =>
{
    options.MaximumNumberOfResults = 8;
    options.FilterExpression =
        "path:\"https://contoso.sharepoint.com/sites/Engineering/\"";
});
```

`HostDelegatedTokenProvider` is implemented by the application. The console and ASP.NET Core samples show Azure Identity and Microsoft Identity Web implementations respectively.

Then:

```csharp
IChatClient retrievalChatClient = new ChatClientBuilder(chatClient)
    .UseMicrosoft365Retrieval(
        TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke)
    .Build(serviceProvider);

var agent = retrievalChatClient.AsAIAgent(
    new ChatClientAgentOptions
    {
        ChatOptions = new()
        {
            Instructions = """
                You are an internal company assistant.
                Use retrieved company knowledge when relevant.
                Cite the source document whenever available.
                """
        }
    },
    services: serviceProvider);
```

Do not optimize for fewer lines at the cost of hiding security, authentication, or Agent Framework concepts.

---

## 31. Architecture Decisions

### ADR-001 — Monorepo prepared for multiple NuGet packages

Decision:

Use one repository named `agent-framework-extensions` with independent package projects under `src/`.

Reason:

The intended scope is a collection of useful Agent Framework extensions, not a single Retrieval API library.

Do not create empty future packages.

### ADR-002 — First package is Microsoft 365 Retrieval-specific

Decision:

Use:

```text
Acterion.Agents.AI.Microsoft365.Retrieval
```

instead of:

```text
Acterion.Agents.AI.Retrieval
```

Reason:

"Retrieval" alone is too generic and could imply Azure AI Search, vector databases, web search, or arbitrary RAG.

### ADR-003 — Reuse Agent Framework `TextSearchProvider`

Decision:

Adapt M365 Retrieval results to `TextSearchProvider.TextSearchResult`.

Reason:

Agent Framework already implements the retrieval lifecycle, context injection, recent-message support, citation prompting, and on-demand search behavior.

### ADR-004 — Delegated identity only

Decision:

Use the current authenticated user's delegated Graph identity.

Reason:

The M365 Retrieval API requires delegated permissions for SharePoint and preserves user-level Microsoft 365 permission trimming.

### ADR-005 — Host owns token acquisition

Decision:

The package consumes delegated tokens through `IMicrosoft365RetrievalTokenProvider`. The host selects credentials, acquires and caches tokens, and owns login, consent UI, Conditional Access challenges, and endpoint authorization.

Reason:

Identity flows vary by host type, and identity SDK dependencies do not belong in the reusable Retrieval package.

### ADR-006 — No speculative Core package

Decision:

Do not create `Acterion.Agents.AI.Core`, `Common`, or `Abstractions` in v0.1.

Reason:

There is no shared cross-package code yet.

### ADR-007 — Feature-oriented specification flow

Decision:

Keep this document as the global source of truth and implement v0.1 through five feature specifications under `specs/features/`.

Reason:

The package is small, but HTTP integration, Agent Framework adaptation, delegated identity, typed SharePoint filtering, and the reference host have different contracts and risks. Separate feature gates keep each implementation session focused without turning individual classes or cross-cutting release work into artificial features.

### ADR-008 — Direct Retrieval API integration, not a Foundry replacement

Decision:

Integrate the Microsoft 365 Copilot Retrieval API directly through `TextSearchProvider` without requiring Foundry Agent Service, a Foundry SharePoint Project Connection, Foundry IQ, a Knowledge Base, or Azure AI Search.

Reason:

The package serves an intentionally narrow scenario: a .NET application already using Microsoft Agent Framework that needs permission-trimmed Microsoft 365 grounding and direct Retrieval API controls. Foundry Agent Service SharePoint Tool and Foundry IQ are valid Microsoft alternatives for their respective managed-tool and managed-knowledge-platform scenarios. The package does not reproduce Foundry IQ capabilities such as knowledge sources, knowledge bases, multi-source retrieval, query planning, routing, reranking, or managed indexing.

---

## 32. MVP Acceptance Criteria

The MVP is complete when all of the following are true:

- [ ] Repository is named `agent-framework-extensions`.
- [ ] Solution is `Acterion.Agents.AI.slnx`.
- [ ] NuGet project is `Acterion.Agents.AI.Microsoft365.Retrieval`.
- [ ] Package builds successfully in Release configuration.
- [ ] Unit tests pass.
- [ ] Package can call `POST /v1.0/copilot/retrieval`.
- [ ] SharePoint is the supported initial data source.
- [ ] Query length is validated.
- [ ] Maximum result count is validated.
- [ ] `filterExpression` is supported.
- [ ] Typed `Path` and `SiteID` filters compose deterministically with `OR`.
- [ ] `resourceMetadata` is supported.
- [ ] Results map to `TextSearchProvider.TextSearchResult`.
- [ ] `ChatClientBuilder.UseMicrosoft365Retrieval` attaches retrieval without manual service resolution or `TextSearchProvider` construction before `AsAIAgent` creates the agent.
- [ ] Source name and source link are preserved.
- [ ] Multiple extracts are handled.
- [ ] The package supports a delegated token provider abstraction.
- [ ] The base package has no dependency on Azure Identity, Microsoft Identity Web, MSAL, or ASP.NET Core.
- [ ] No application-permission fallback exists.
- [ ] No tokens or retrieved document text are logged by default.
- [ ] A console sample demonstrates delegated device-code authentication with Azure Identity and Agent Framework.
- [ ] An ASP.NET Core sample demonstrates delegated OBO authentication with Microsoft Identity Web and Agent Framework.
- [ ] Identity adapters are owned by their sample hosts.
- [ ] Both samples demonstrate automatic retrieval.
- [ ] The documentation explains on-demand retrieval.
- [ ] README documents required delegated Graph permissions.
- [ ] README documents `filterExpression` security caveat.
- [ ] README documents indirect prompt injection risk.
- [ ] CI builds, tests, and packs the solution.
- [ ] `dotnet pack` generates a valid NuGet package with Acterion metadata, icon, README, and MIT license.
- [ ] No unused speculative project exists.

---

## 33. Release Delivery Checklist

Implementation agents should execute these tasks incrementally and keep the repository buildable after each major step.

The feature specifications are the implementation gates for phases 2 through 7:

| Feature specification | Global phases primarily covered | Depends on |
| --- | --- | --- |
| `001-sharepoint-retrieval-client/001-sharepoint-retrieval-client.md` | 2, 3, and relevant parts of 6 | Repository foundation |
| `002-agent-framework-integration/002-agent-framework-integration.md` | 4 and relevant parts of 6 | Feature 001 |
| `003-console-reference-sample/003-console-reference-sample.md` | 5 and relevant parts of 6 | Features 001 and 002 |
| `004-aspnetcore-reference-sample/004-aspnetcore-reference-sample.md` | 7 | Features 001 and 002 |
| `005-typed-sharepoint-retrieval-filters/005-typed-sharepoint-retrieval-filters.md` | 3 and relevant parts of 6 | Feature 001 options contract |

Phases 8 through 10 are release-level completion work and MUST be validated after all five feature specifications are complete.

Repository foundation, package metadata, Source Link, package README/icon inclusion, CI workflow ownership, root documentation, and final security/quality review belong to this release checklist. Feature specifications may require their own documentation or buildability, but MUST NOT own repository-wide publishing or release automation.

### Phase 1 — Repository foundation

- [ ] Inspect the current repository folder before creating files.
- [ ] Preserve any existing branding assets and repository configuration.
- [ ] Create `Acterion.Agents.AI.slnx`.
- [ ] Create the initial package project under `src/`.
- [ ] Create the unit test project under `tests/`.
- [ ] Create the console sample under `samples/`.
- [ ] Create the ASP.NET Core sample under `samples/`.
- [ ] Add all projects to the solution.
- [ ] Add `Directory.Build.props`.
- [ ] Add `Directory.Packages.props`.
- [ ] Add MIT `LICENSE` if one does not already exist.
- [ ] Add/update `.gitignore`.
- [ ] Ensure `dotnet restore` and `dotnet build` succeed.

### Phase 2 — Package foundation

- [ ] Add minimal package dependencies.
- [ ] Configure NuGet metadata.
- [ ] Include existing Acterion package icon/logo if present.
- [ ] Add XML documentation generation.
- [ ] Configure Source Link/repository metadata.
- [ ] Add options class and validation.
- [ ] Add token provider abstraction.

### Phase 3 — Retrieval API client

- [ ] Implement typed `HttpClient`.
- [ ] Implement v1.0 Retrieval API request DTO.
- [ ] Implement response DTOs.
- [ ] Implement request validation.
- [ ] Implement bearer token injection.
- [ ] Implement cancellation.
- [ ] Implement error handling.
- [ ] Preserve request/correlation identifiers when useful.
- [ ] Ensure secrets/content are not included in exceptions or logs.

### Phase 4 — Agent Framework adapter

- [ ] Implement `Microsoft365RetrievalSearch`.
- [ ] Map retrieval hits to `TextSearchProvider.TextSearchResult`.
- [ ] Preserve source title and URL.
- [ ] Concatenate multiple extracts.
- [ ] Preserve raw representation.
- [ ] Support empty result sets.
- [ ] Add `UseMicrosoft365Retrieval` overloads for explicit behavior and full `TextSearchProviderOptions` configuration.

### Phase 5 — Console reference sample

- [ ] Create the executable console sample.
- [ ] Configure Azure Identity device-code authentication in the sample host.
- [ ] Implement a sample-local `IMicrosoft365RetrievalTokenProvider` adapter.
- [ ] Acquire a delegated Graph token for `Files.Read.All` and `Sites.Read.All`.
- [ ] Fail clearly when no delegated identity/token is available.
- [ ] Do not use application identity or automatic credential fallback.
- [ ] Add tenant-independent tests using a fake credential or token provider.

### Phase 6 — Tests

- [ ] Add request validation tests.
- [ ] Add JSON serialization tests.
- [ ] Add response mapping tests.
- [ ] Add HTTP error tests.
- [ ] Add cancellation tests.
- [ ] Add token/header tests.
- [ ] Add logging/security tests where practical.
- [ ] Keep all normal CI tests tenant-independent.

### Phase 7 — Sample

- [ ] Configure Entra-authenticated ASP.NET Core API.
- [ ] Configure Microsoft.Identity.Web OBO.
- [ ] Implement the Microsoft Identity Web token adapter inside the sample.
- [ ] Configure Microsoft Agent Framework agent.
- [ ] Configure M365 Retrieval adapter.
- [ ] Add authenticated `/api/assistant` endpoint.
- [ ] Demonstrate SharePoint site scoping.
- [ ] Use secure configuration practices.
- [ ] Add a sample README with app registration steps.

### Phase 8 — Documentation

- [ ] Write root README.
- [ ] Add package table.
- [ ] Add quick-start code.
- [ ] Document Entra app registration.
- [ ] Document delegated Graph permissions.
- [ ] Document Retrieval API prerequisites/licensing at a high level.
- [ ] Document automatic retrieval.
- [ ] Document on-demand retrieval.
- [ ] Document filter scoping.
- [ ] Add Security Considerations section.
- [ ] Add project disclaimer.
- [ ] Add contribution guidance.

### Phase 9 — CI and packaging

- [ ] Add GitHub Actions build/test/pack workflow.
- [ ] Validate clean checkout build.
- [ ] Validate NuGet output.
- [ ] Validate package README/icon.
- [ ] Ensure no secrets are required for CI.
- [ ] Produce initial `0.1.0` package locally.

### Phase 10 — Final quality pass

- [ ] Remove unused abstractions.
- [ ] Remove speculative code.
- [ ] Check public API naming consistency.
- [ ] Check nullable reference annotations.
- [ ] Check cancellation propagation.
- [ ] Check logging for sensitive values.
- [ ] Run all tests.
- [ ] Run `dotnet pack`.
- [ ] Verify sample compiles.
- [ ] Verify README commands match the actual code.

---

## 34. Implementation Working Rules

When implementing this specification:

1. Inspect existing files before overwriting anything.
2. Do not delete existing Acterion branding assets.
3. Prefer official Microsoft libraries and platform capabilities.
4. Keep the implementation small.
5. Do not create features that are only listed as future possibilities.
6. Do not create a `Core` project.
7. Keep code, identifiers, comments, logs, tests, and documentation in English.
8. Follow standard .NET naming conventions.
9. Use nullable reference types.
10. Use async APIs end-to-end.
11. Accept `CancellationToken` on I/O operations.
12. Do not log tokens or retrieved document contents.
13. Do not commit credentials.
14. Avoid app-only Microsoft Graph authentication for Retrieval.
15. Do not treat KQL filtering as authorization.
16. Prefer composition over custom framework abstractions.
17. Reuse `TextSearchProvider`.
18. Before adding a dependency, justify why the existing BCL/Microsoft stack is insufficient.
19. Keep the public API surface deliberately small.
20. If the current Agent Framework API differs from examples in this spec, adapt to the current stable API while preserving the architectural intent and document the change.

---

## 35. Future Ideas — Explicitly Out of Scope for v0.1

These are possible future extensions, not implementation tasks:

### Microsoft Graph context providers

A future package such as:

```text
Acterion.Agents.AI.MicrosoftGraph
```

could contain:

```text
UserProfileContextProvider
OrganizationContextProvider
PeopleContextProvider
```

### SharePoint-specific integrations

A future package such as:

```text
Acterion.Agents.AI.SharePoint
```

could provide Agent Framework features that use SharePoint APIs directly and are not part of the Microsoft 365 Retrieval API.

### Additional Retrieval API data sources

Future versions of:

```text
Acterion.Agents.AI.Microsoft365.Retrieval
```

may add:

```text
OneDrive for Business
Copilot Connectors
```

only after the SharePoint integration is stable.

### Observability

OpenTelemetry Activities and Metrics may be added later.

None of these should be implemented in the initial pull request.

---

## 36. Authoritative References

Use current Microsoft documentation during implementation and prefer stable APIs.

Microsoft Agent Framework:

- Repository: https://github.com/microsoft/agent-framework
- RAG / `TextSearchProvider`: https://learn.microsoft.com/en-us/agent-framework/agents/rag
- `TextSearchProvider` API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.ai.textsearchprovider

Microsoft 365 Copilot Retrieval API:

- Overview: https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/overview
- API reference: https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/copilotroot-retrieval

Microsoft Foundry SharePoint and Foundry IQ:

- Foundry Agent Service SharePoint Tool: https://learn.microsoft.com/en-us/azure/foundry/agents/how-to/tools/sharepoint
- What is Foundry IQ?: https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/what-is-foundry-iq
- Foundry IQ FAQ: https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/foundry-iq-faq
- Remote SharePoint knowledge source / SharePoint indexer guidance: https://learn.microsoft.com/en-us/azure/search/search-how-to-index-sharepoint-online
- Indexed SharePoint knowledge source (Preview): https://learn.microsoft.com/en-us/azure/search/agentic-knowledge-source-how-to-sharepoint-indexed

Microsoft Identity Web:

- Documentation: https://learn.microsoft.com/en-us/entra/msidweb/

Azure Identity:

- Credential chains and credential selection: https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/credential-chains
- `DeviceCodeCredential`: https://learn.microsoft.com/en-us/dotnet/api/azure.identity.devicecodecredential
- `OnBehalfOfCredential`: https://learn.microsoft.com/en-us/dotnet/api/azure.identity.onbehalfofcredential

Before coding against an API shape, or documenting concrete parameter names, limits, supported scopes, or Foundry limitations, verify the current official Microsoft documentation. Agent Framework, Foundry Agent Service, Foundry IQ, and the Retrieval API are actively evolving; clearly mark Preview behavior and do not treat undocumented behavior as a stable contract.

---

## 37. Definition of Success

The project succeeds if a .NET developer already using Microsoft Agent Framework can install:

```text
Acterion.Agents.AI.Microsoft365.Retrieval
```

and, with a small amount of configuration, turn this:

```text
Authenticated employee
        ↓
Agent Framework agent
```

into this:

```text
Authenticated employee
        ↓
Agent Framework agent
        ↓
Microsoft 365 Copilot Retrieval API
        ↓
Permission-trimmed SharePoint knowledge
        ↓
Grounded answer with citations
```

without building a separate SharePoint ingestion and vector-search pipeline, and without learning a new custom agent abstraction.
