# Feature 001: SharePoint Retrieval Client

**Parent specification:** [`../SPEC.md`](../SPEC.md)  
**Status:** Draft  
**Depends on:** Repository foundation  
**Enables:** Features 002, 003, 004, and 005

## Objective

Provide a small, host-independent .NET client that sends a validated natural-language query to the Microsoft 365 Copilot Retrieval API and returns typed SharePoint retrieval hits.

A package consumer supplies a delegated Microsoft Graph token through an abstraction. The client owns request construction, transport, deserialization, diagnostics, and API error translation. It does not own login, consent, endpoint authorization, or Agent Framework result mapping.

## User Outcome

A developer can inject `IMicrosoft365RetrievalClient`, call its asynchronous retrieval operation with a query and cancellation token, and receive zero or more typed hits without handling HTTP or JSON directly.

## Global Requirements Inherited

This feature MUST comply with the parent specification, especially:

- §8, Microsoft 365 Copilot Retrieval API;
- §11.1 and §11.2, options and retrieval client responsibilities;
- §12.1, §12.2, and §12.4, delegated identity and token abstraction;
- §13, authorization and security model;
- §14, HTTP and resilience;
- §15, dependency injection;
- §18 and §19, dependencies and .NET 10 baseline;
- §23.1, client-related unit tests;
- §24 and §25, errors and observability;
- §28 and §34, simplicity and implementation rules.

If this document conflicts with the parent specification, the parent specification wins.

## Scope

### In Scope

- `Microsoft365RetrievalOptions` with SharePoint-specific v0.1 defaults.
- `IMicrosoft365RetrievalTokenProvider` as the host boundary for delegated access tokens.
- `IMicrosoft365RetrievalClient` and its HTTP implementation.
- Internal request and response DTOs for the v1.0 REST contract.
- A minimal typed result model sufficient for later Agent Framework mapping.
- Client-side validation, cancellation, safe logging, and error translation.
- DI registration for options, `HttpClient`, the client, and the search-independent services owned by this feature.
- Tenant-independent unit tests using fake token and HTTP handlers.

### Out of Scope

- `Microsoft.Identity.Web` token acquisition implementation; see Feature 003.
- `TextSearchProvider` or any Agent Framework mapping; see Feature 002.
- ASP.NET Core authentication or endpoints; see Feature 004.
- OneDrive, Copilot connectors, thumbnails, batching, caching, and live-tenant tests.
- Interactive authentication, consent, claims challenges, or app-only fallback.
- Custom retry or authorization frameworks.

## Functional Requirements

### Options

`Microsoft365RetrievalOptions` MUST expose:

- `MaximumNumberOfResults`, default `8`, valid from `1` through `25` inclusive;
- optional `FilterExpression`;
- `ResourceMetadata`, defaulting to `title` and `author`.

The data source is fixed internally to `sharePoint` for v0.1. No public data-source enum is introduced.

Configured options MUST be validated when the service graph is resolved. Query-specific validation MUST also occur at the public retrieval boundary.

### Token Boundary

`IMicrosoft365RetrievalTokenProvider` MUST provide:

```csharp
Task<string> GetAccessTokenAsync(
    CancellationToken cancellationToken = default);
```

The client MUST request a token for each operation through this abstraction and MUST NOT inspect `HttpContext`, persist tokens, or fall back to another identity.

### Retrieval Operation

The public client operation MUST:

- accept a query string and `CancellationToken`;
- reject null, empty, whitespace-only, or over-1,500-character queries before token acquisition or network I/O;
- issue `POST https://graph.microsoft.com/v1.0/copilot/retrieval`;
- send JSON containing `queryString`, `dataSource`, configured `filterExpression`, `resourceMetadata`, and `maximumNumberOfResults`;
- send the delegated token using the Bearer authorization scheme;
- return an empty collection for an empty `retrievalHits` array;
- tolerate unknown JSON response fields;
- honor cancellation during token acquisition, send, and response deserialization.

The public client contract MUST return:

```csharp
Task<IReadOnlyList<Microsoft365RetrievalHit>> RetrieveAsync(
    string query,
    CancellationToken cancellationToken = default);
```

`Microsoft365RetrievalHit` is the single public result model crossing the Feature 001/002 boundary. It MUST be read-only to consumers and expose:

- `WebUrl` as the original response string;
- `Extracts` as a read-only list of `Microsoft365RetrievalExtract`;
- `ResourceType` as an optional string;
- `ResourceMetadata` as a read-only string-keyed collection whose values preserve JSON scalar values without assuming every requested metadata field is a string.

`Microsoft365RetrievalExtract` MUST be read-only and expose the extract `Text` plus nullable `RelevanceScore`. Internal wire DTOs MAY retain sensitivity-label data for deserialization, but it is not part of the public result contract in v0.1.

The typed hit representation therefore preserves, without making it LLM-visible:

- `webUrl`;
- extracts in response order, including text and optional relevance score;
- `resourceType`;
- requested resource metadata;
- enough requested metadata for source mapping and consumer diagnostics.

Unknown wire fields MUST be ignored safely; preserving them is not a v0.1 contract. Feature 002 assigns the resulting `Microsoft365RetrievalHit` instance to `TextSearchResult.RawRepresentation`.

### HTTP Failures

Non-success responses MUST produce a small package exception carrying the HTTP status and Graph request/correlation identifier when available. Header extraction MUST prefer `request-id`, then `client-request-id`; it MUST NOT parse the response body merely to obtain diagnostics.

Messages MUST distinguish actionable categories for `400`, `401`, `403`, `429`, and `5xx` without including authorization headers, tokens, raw response bodies, queries, filters, or retrieved content.

The feature MUST NOT retry `400`, `401`, or `403`. Any retry support MUST use standard .NET HTTP resilience and respect `Retry-After`; retries are optional for v0.1.

### Logging

Information-level logs MAY include operation start/completion, elapsed duration, status, result count, maximum results, and whether a filter is configured.

Default logs MUST NOT include token values, authorization headers, full queries, filter contents, extracts, metadata values, or raw bodies.

## Dependency Injection Contract

The feature MUST provide a thin `IServiceCollection` registration method that configures a typed `HttpClient` and validates options. Registration MUST require the host to provide an `IMicrosoft365RetrievalTokenProvider` unless an explicit integration such as Feature 003 is selected.

Registration MUST NOT alter authentication, authorization, token caches, or unrelated global HTTP configuration.

## Code Conventions

- Target `net10.0`, enable nullable reference types, and use async APIs end to end.
- Use `System.Net.Http` and `System.Text.Json`; do not add the Microsoft Graph SDK for this endpoint.
- Keep wire DTOs internal unless a public type is required by the client contract.
- Prefer sealed types and read-only results where they reduce accidental mutation.
- Do not add generic provider, repository, mediator, result-monad, or resilience abstractions.

## Testing Strategy

Unit tests MUST cover:

- all query and result-count boundary cases;
- exact request method, endpoint, headers, content type, and JSON properties;
- token-provider invocation and Bearer header application;
- empty hits, multiple extracts, missing optional fields, and unknown fields;
- cancellation at the token and HTTP boundaries;
- representative non-success statuses and request-id preservation;
- absence of tokens, authorization headers, response content, and full query/filter values from exceptions and default logs.

Normal tests MUST use in-memory fakes and require no tenant, credentials, network access, or Microsoft 365 license.

## Commands

```powershell
dotnet build Acterion.Agents.AI.slnx --configuration Release
dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release
```

Focused test filters MUST use the xUnit v3 Microsoft Testing Platform syntax supported by .NET 10.

Example class filter:

```powershell
dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*Microsoft365RetrievalClientTests"
```

## Boundaries

### Always

- Validate before token acquisition and network I/O.
- Treat external responses as untrusted input.
- Preserve delegated-user isolation and propagate cancellation.
- Add tests for every observable HTTP or error contract.

### Ask First

- Add a new package dependency.
- Make wire DTOs public.
- Add automatic retries or result caching.
- Expand beyond SharePoint.

### Never

- Log or expose access tokens or retrieved document content.
- Use app-only permissions or a privileged fallback identity.
- Treat `filterExpression` as authorization.
- Concatenate arbitrary end-user input into configured KQL.

## Acceptance Criteria

- [ ] A valid query produces the documented v1.0 SharePoint request using a delegated Bearer token.
- [ ] Invalid local input fails before token acquisition or HTTP I/O.
- [ ] Successful responses return typed hits and zero hits return an empty collection.
- [ ] Cancellation and representative Graph failures have tested, safe behavior.
- [ ] Default logs and exceptions contain no credentials, raw bodies, or retrieved content.
- [ ] The focused tests and full Release build pass without live credentials.

## Plan Gate Exit Evidence

- Compile the specified public client/result contract from a separate consumer project or API-surface test.
- Record whether standard HTTP resilience is included. Default to deferring retries unless a focused test proves correct `Retry-After`, cancellation, and non-retryable-status behavior without a custom framework.
- Compile request serialization against the centrally pinned package graph and prove all focused tests run with the documented MTP command.
