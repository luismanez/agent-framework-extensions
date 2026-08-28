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
3. [`003-microsoft-identity-web-integration.md`](../features/003-microsoft-identity-web-integration/003-microsoft-identity-web-integration.md) — delegated token acquisition through `Microsoft.Identity.Web`;
4. [`004-aspnetcore-reference-sample.md`](../features/004-aspnetcore-reference-sample/004-aspnetcore-reference-sample.md) — the end-to-end ASP.NET Core reference application;
5. [`005-typed-sharepoint-retrieval-filters.md`](../features/005-typed-sharepoint-retrieval-filters/005-typed-sharepoint-retrieval-filters.md) — typed construction of trusted SharePoint path and site-ID filters.

Each feature MUST pass its own specify, plan, tasks, and implementation gates before it is considered complete. Feature specifications refine this document but MUST NOT override it. If a conflict is found, update or clarify the global specification first, then align the affected feature specification.

CI, packaging, repository-wide documentation, security review, and final quality checks remain release-level concerns in this document. They are not separate product features.

---

## 2. High-Level Goals

The initial package MUST:

1. Make it easy for a .NET developer using Microsoft Agent Framework to ground an agent with content retrieved through the Microsoft 365 Copilot Retrieval API.
2. Reuse the Agent Framework `TextSearchProvider` model instead of implementing a parallel RAG lifecycle.
3. Support user-delegated Microsoft Entra ID authentication, including the common ASP.NET Core + `Microsoft.Identity.Web` On-Behalf-Of scenario.
4. Preserve Microsoft 365 permission trimming by calling the Retrieval API using the current user's delegated identity.
5. Provide source URLs and useful source names so Agent Framework can generate citations.
6. Support both Agent Framework retrieval behaviors:
   - retrieval before the model invocation;
   - retrieval exposed as an on-demand function/tool.
7. Be small, understandable, testable, and production-oriented.
8. Keep package-level dependencies and public APIs minimal.
9. Prepare the repository for future `Acterion.Agents.AI.*` NuGet packages without creating speculative `Core`, `Common`, or `Abstractions` projects.

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
- a replacement for `Microsoft.Identity.Web`;
- a replacement for `TextSearchProvider`;
- OneDrive-specific features;
- Copilot Connector-specific features;
- thumbnail handling;
- Microsoft 365 Copilot licensing or billing management.

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
│       └── ...
│
├── tests/
│   └── Acterion.Agents.AI.Microsoft365.Retrieval.Tests/
│       ├── Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj
│       └── ...
│
├── samples/
│   └── Microsoft365Retrieval.AspNetCore/
│       ├── Microsoft365Retrieval.AspNetCore.csproj
│       └── ...
│
├── .github/
│   └── workflows/
│
├── Directory.Build.props
├── Directory.Packages.props
├── Acterion.Agents.AI.slnx
├── README.md
├── LICENSE
└── .gitignore
```

The structure MUST make adding another package later as simple as adding another project under `src/`, its tests under `tests/`, and optional samples under `samples/`.

---

## 6. Product Positioning

Suggested README introduction:

> **Agent Framework Extensions** provides community-driven .NET extensions and integrations for Microsoft Agent Framework.
>
> The first package, `Acterion.Agents.AI.Microsoft365.Retrieval`, connects Agent Framework agents to the Microsoft 365 Copilot Retrieval API, enabling permission-aware grounding over Microsoft 365 content such as SharePoint.

The project should explicitly state that it is a community project and is **not an official Microsoft package**.

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

Feature 005 provides optional typed helpers for trusted SharePoint path and site-ID filters. The raw `FilterExpression` option remains available for advanced KQL scenarios.

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

A convenience API MAY be implemented if it remains thin and does not hide important Agent Framework behavior.

A desirable final developer experience could be similar to:

```csharp
services.AddMicrosoft365Retrieval(options =>
{
    options.MaximumNumberOfResults = 8;
    options.FilterExpression =
        "path:\"https://contoso.sharepoint.com/sites/Engineering/\"";
});
```

and:

```csharp
var provider = serviceProvider
    .GetRequiredService<Microsoft365RetrievalProvider>();

AIAgent agent = chatClient.AsAIAgent(
    new ChatClientAgentOptions
    {
        ChatOptions = new()
        {
            Instructions =
                "You are an internal company assistant."
        },
        AIContextProviders =
        [
            provider.CreateTextSearchProvider(
                new TextSearchProviderOptions
                {
                    SearchTime =
                        TextSearchProviderOptions.TextSearchBehavior
                            .BeforeAIInvoke
                })
        ]
    });
```

Do not invent a large custom builder DSL for v0.1.

### 10.3 Retrieval behaviors

The integration MUST be usable with both Agent Framework modes:

#### Automatic

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

#### On demand

```csharp
SearchTime =
    TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling;
```

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
```

Optionally:

```text
MicrosoftIdentityWebRetrievalTokenProvider
Microsoft365RetrievalProvider
```

if these materially simplify the common ASP.NET Core scenario.

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

## 12. Authentication and Microsoft Entra ID

### 12.1 Required authentication model

The Microsoft 365 Retrieval API requires a **delegated work/school user identity** for the SharePoint scenario.

Application permissions are not supported for this API.

The package MUST therefore be designed around delegated user tokens.

### 12.2 Required Graph permissions for SharePoint

The host application must acquire delegated Microsoft Graph permissions:

```text
Files.Read.All
Sites.Read.All
```

Both are required for SharePoint retrieval.

The package README MUST explicitly document the privilege implications of these permissions.

### 12.3 ASP.NET Core + Microsoft.Identity.Web

The primary sample should demonstrate:

```text
Browser/client
    │
    │ token for host API
    ▼
ASP.NET Core API
    │
    │ Microsoft.Identity.Web
    │ delegated token acquisition / OBO
    ▼
Microsoft Graph
    │
    ▼
M365 Copilot Retrieval API
```

Suggested host setup:

```csharp
builder.Services
    .AddMicrosoftIdentityWebApiAuthentication(
        builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();
```

The sample may use an in-memory token cache for simplicity.

Documentation MUST state that multi-instance production applications should use an appropriate distributed token cache according to their hosting architecture.

### 12.4 Token acquisition abstraction

The retrieval client should not be tightly coupled to `HttpContext`.

Use a minimal token provider abstraction:

```csharp
public interface IMicrosoft365RetrievalTokenProvider
{
    Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken = default);
}
```

The package may provide an implementation backed by `Microsoft.Identity.Web`.

This keeps the retrieval client:

- testable;
- host-independent;
- reusable outside a Minimal API;
- easy to adapt to future hosting models.

Do not create a separate authentication NuGet package for this abstraction.

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
- search adapter;
- token provider when Microsoft.Identity.Web integration is enabled.

Avoid surprising global service registrations.

Do not alter the host application's authentication or authorization configuration automatically.

---

## 16. Sample Application

Create:

```text
samples/Microsoft365Retrieval.AspNetCore
```

The sample should be intentionally small and production-inspired.

### 16.1 Scenario

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

### 16.2 Sample requirements

The sample MUST demonstrate:

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

### 16.3 Model provider

The library itself MUST remain independent of the model provider.

The sample may use Azure OpenAI / Microsoft Foundry for convenience, but package code must not require it.

Prefer Entra-based authentication such as `DefaultAzureCredential`/managed identity for the model service instead of API keys where practical.

---

## 17. Configuration Example

Example only:

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

Production guidance should prefer certificates, workload identity, managed identity where applicable, or the deployment-specific secure credential mechanism supported by `Microsoft.Identity.Web`.

---

## 18. Package Dependencies

Keep dependencies minimal.

Expected direct dependencies may include:

```text
Microsoft.Agents.AI
Microsoft.Identity.Web
Microsoft.Extensions.Http
Microsoft.Extensions.Options.ConfigurationExtensions
```

Only add a dependency if package code directly requires it.

Do not reference the Microsoft Graph SDK merely to call one Retrieval API endpoint unless it provides a clear implementation or maintenance advantage.

For the MVP, a small typed `HttpClient` over the stable v1.0 REST endpoint is preferred.

Centralize package versions using:

```text
Directory.Packages.props
```

---

## 19. Target Frameworks

The initial package, tests, and sample target:

```text
net10.0
```

The initial implementation should prefer a **single practical target framework** over unnecessary multi-targeting.

Do not target an out-of-support runtime merely for historical compatibility.

The initial dependency baseline, verified when the repository scaffold was created, is:

```text
Microsoft.Agents.AI 1.19.0
Microsoft.Identity.Web 4.14.2
```

Package versions MUST remain centrally managed in `Directory.Packages.props`. Before implementing a feature, verify that these are still the latest stable compatible versions. A version update is allowed when it preserves the architectural intent and the complete solution remains buildable and testable.

Do not use preview package versions by default.

### 19.1 Canonical commands

The canonical repository verification commands are:

```powershell
dotnet restore Acterion.Agents.AI.slnx
dotnet build Acterion.Agents.AI.slnx --configuration Release --no-restore
dotnet test --solution Acterion.Agents.AI.slnx --configuration Release --no-build
dotnet pack src/Acterion.Agents.AI.Microsoft365.Retrieval/Acterion.Agents.AI.Microsoft365.Retrieval.csproj --configuration Release --no-build --output artifacts/packages
dotnet run --project samples/Microsoft365Retrieval.AspNetCore/Microsoft365Retrieval.AspNetCore.csproj
```

The test command uses Microsoft Testing Platform selected in `global.json`. Until the first test is added, Microsoft Testing Platform exits with code `8` for zero discovered tests; feature implementation MUST replace that temporary scaffold state with executable tests.

---

## 20. NuGet Packaging

Common NuGet metadata should live in:

```text
Directory.Build.props
```

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
    <PackageIcon>icon.png</PackageIcon>

    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <ContinuousIntegrationBuild
        Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
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

The sample must compile as part of CI.

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
8. sample configuration;
9. automatic retrieval example;
10. on-demand retrieval example;
11. SharePoint site scoping example;
12. security considerations;
13. Microsoft 365 licensing prerequisite note;
14. link to the sample;
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
├── DependencyInjection/
│   └── Microsoft365RetrievalServiceCollectionExtensions.cs
│
├── Authentication/
│   ├── IMicrosoft365RetrievalTokenProvider.cs
│   └── MicrosoftIdentityWebRetrievalTokenProvider.cs
│
├── Http/
│   ├── Microsoft365RetrievalClient.cs
│   └── RetrievalApiModels.cs
│
├── Microsoft365RetrievalOptions.cs
├── Microsoft365RetrievalSearch.cs
└── Microsoft365RetrievalException.cs
```

If fewer folders make the package easier to navigate, prefer fewer folders.

---

## 30. Expected Developer Experience

The final README should make the common path feel approximately this simple:

```csharp
builder.Services
    .AddMicrosoftIdentityWebApiAuthentication(
        builder.Configuration)
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddMicrosoft365Retrieval(options =>
{
    options.MaximumNumberOfResults = 8;
    options.FilterExpression =
        "path:\"https://contoso.sharepoint.com/sites/Engineering/\"";
});
```

Then:

```csharp
var retrieval = serviceProvider
    .GetRequiredService<Microsoft365RetrievalSearch>();

var contextProvider = new TextSearchProvider(
    retrieval.SearchAsync,
    new TextSearchProviderOptions
    {
        SearchTime =
            TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke,
        RecentMessageMemoryLimit = 3
    });

var agent = chatClient.AsAIAgent(
    new ChatClientAgentOptions
    {
        ChatOptions = new()
        {
            Instructions = """
                You are an internal company assistant.
                Use retrieved company knowledge when relevant.
                Cite the source document whenever available.
                """
        },
        AIContextProviders = [contextProvider]
    });
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

### ADR-005 — Host owns authentication UX

Decision:

The package acquires/uses delegated tokens but does not implement login, consent UI, Conditional Access challenges, or endpoint authorization.

Reason:

These belong to the host application's authentication boundary.

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
- [ ] Source name and source link are preserved.
- [ ] Multiple extracts are handled.
- [ ] The package supports a delegated token provider abstraction.
- [ ] Microsoft.Identity.Web integration is demonstrated.
- [ ] No application-permission fallback exists.
- [ ] No tokens or retrieved document text are logged by default.
- [ ] An ASP.NET Core sample demonstrates OBO + Agent Framework.
- [ ] The sample demonstrates automatic retrieval.
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
| `003-microsoft-identity-web-integration/003-microsoft-identity-web-integration.md` | 5 and relevant parts of 6 | Feature 001 |
| `004-aspnetcore-reference-sample/004-aspnetcore-reference-sample.md` | 7 | Features 001, 002, and 003 |
| `005-typed-sharepoint-retrieval-filters/005-typed-sharepoint-retrieval-filters.md` | 3 and relevant parts of 6 | Feature 001 options contract |

Phases 8 through 10 are release-level completion work and MUST be validated after all five feature specifications are complete.

Repository foundation, package metadata, Source Link, package README/icon inclusion, CI workflow ownership, root documentation, and final security/quality review belong to this release checklist. Feature specifications may require their own documentation or buildability, but MUST NOT own repository-wide publishing or release automation.

### Phase 1 — Repository foundation

- [ ] Inspect the current repository folder before creating files.
- [ ] Preserve any existing branding assets and repository configuration.
- [ ] Create `Acterion.Agents.AI.slnx`.
- [ ] Create the initial package project under `src/`.
- [ ] Create the unit test project under `tests/`.
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
- [ ] Add helper/factory API only if it clearly reduces boilerplate.

### Phase 5 — Microsoft.Identity.Web integration

- [ ] Implement the Microsoft.Identity.Web token provider.
- [ ] Acquire delegated Graph token for `Files.Read.All` and `Sites.Read.All`.
- [ ] Fail clearly when no delegated identity/token is available.
- [ ] Do not implement interactive consent/challenge behavior.
- [ ] Add tests using mocked token provider.

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

Microsoft Identity Web:

- Documentation: https://learn.microsoft.com/en-us/entra/msidweb/

Before coding against an API shape, verify the current stable Microsoft documentation because Agent Framework and the Retrieval API are actively evolving.

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
