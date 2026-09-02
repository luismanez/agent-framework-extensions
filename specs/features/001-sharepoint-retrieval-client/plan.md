# Implementation Plan: Feature 001 - SharePoint Retrieval Client

## Overview

Implement the host-independent .NET 10 client that validates a natural-language query, obtains a delegated Microsoft Graph token through the package abstraction, calls `POST /v1.0/copilot/retrieval`, and returns immutable typed SharePoint hits. Work proceeds contract-first and test-first, with no live tenant, Graph SDK, result cache, custom retry framework, or Agent Framework mapping.

This plan is local to Feature 001. The executable checklist is [`todo.md`](todo.md), and the governing specification is [`001-sharepoint-retrieval-client.md`](001-sharepoint-retrieval-client.md).

## Planning Baseline

- Repository build succeeds in Release on .NET SDK `10.0.302` using feature band `10.0.300`.
- The source and test projects contain no `.cs` implementation yet.
- Tests use xUnit v3 `4.0.0` on Microsoft Testing Platform.
- `Microsoft.Agents.AI 1.19.0` and `Microsoft.Identity.Web 4.14.2` remain the latest stable versions at planning time.
- Feature 001 does not directly use Agent Framework or Microsoft Identity Web APIs, but it remains in the existing package that will host Features 002 and 003.
- The referenced shared Definition of Done file is not present in the installed planning skill. This plan therefore defines its task-level Definition of Done explicitly below.

## Scope Boundaries

### In Scope

- Public options, token-provider, client, result, extract, and exception contracts.
- A typed `HttpClient` implementation over the stable v1.0 Retrieval API endpoint.
- Internal wire DTOs and `System.Text.Json` serialization/deserialization.
- Local validation, delegated Bearer token application, cancellation, safe diagnostics, and HTTP error translation.
- DI registration and option validation.
- Tenant-independent unit and consumer-contract tests.
- XML documentation for public Feature 001 APIs and a feature-local implementation evidence record.

### Out of Scope

- Microsoft Identity Web token acquisition implementation.
- `TextSearchProvider` mapping or agent construction.
- ASP.NET Core endpoint authentication or authorization.
- OneDrive, Copilot connectors, thumbnails, batching, caching, or result post-filtering.
- Automatic retries. These are deferred because they are optional and require separate proof for `Retry-After`, cancellation, and non-retryable statuses.
- Live Microsoft 365 tests in normal CI.
- Root README completion, final package-content inspection, publishing metadata, and release packaging gates owned by the parent release checklist.

## Architecture Decisions

### 1. Public Contract

Use the namespace `Acterion.Agents.AI.Microsoft365.Retrieval` and expose:

- `Microsoft365RetrievalOptions`;
- `IMicrosoft365RetrievalTokenProvider`;
- `IMicrosoft365RetrievalClient`;
- `Microsoft365RetrievalClient`;
- `Microsoft365RetrievalHit`;
- `Microsoft365RetrievalExtract`;
- `Microsoft365RetrievalException`;
- one `IServiceCollection` extension for registration.

Public result types are sealed and immutable to consumers. Constructors used only by response mapping should remain non-public unless a consumer-contract test proves public construction is required.

### 2. Metadata Representation

Use `IReadOnlyDictionary<string, JsonElement>` for `Microsoft365RetrievalHit.ResourceMetadata`.

Rationale:

- preserves string, number, Boolean, and null values without coercion;
- adds no dependency beyond `System.Text.Json`;
- avoids a speculative metadata abstraction;
- keeps the Feature 001/002 boundary typed and read-only.

Copy values with `JsonElement.Clone()` before exposing them. Preserve property names with ordinal, case-sensitive semantics. Treat object and array metadata values as an invalid known-field shape rather than silently changing the public scalar contract. Missing `resourceMetadata` maps to an empty read-only dictionary.

### 3. Required and Optional Response Fields

- `webUrl` is required and remains the original response string; missing or non-string values are invalid.
- `extracts` may be missing or empty and maps to an empty read-only list.
- each present extract requires string `text`; `relevanceScore` is nullable.
- `resourceType` is optional.
- `retrievalHits` must be present; an empty array returns an empty result collection.
- unknown JSON properties are ignored.

Malformed successful payloads are wrapped in `Microsoft365RetrievalException` with the HTTP status and inner serialization exception. Cancellation is never wrapped.

### 4. Request Serialization

Use internal DTOs with `System.Text.Json` and web JSON naming. Send:

- `queryString`;
- fixed `dataSource = "sharePoint"`;
- `filterExpression` only when configured;
- `resourceMetadata`;
- `maximumNumberOfResults`.

Use UTF-8 `application/json`. Do not serialize tokens or logging state. Do not introduce source generation until profiling or trimming requirements justify it.

### 5. Validation

At the public retrieval boundary, reject null, empty, whitespace-only, and over-1,500-character queries before token acquisition or HTTP I/O.

Options validation rejects:

- `MaximumNumberOfResults` outside `1..25`;
- a null `ResourceMetadata` collection;
- null, empty, or whitespace-only metadata names.

`FilterExpression` remains trusted raw application configuration and is not parsed or described as authorization. An empty token returned by a provider fails before HTTP I/O.

### 6. HTTP and Error Semantics

- Configure a typed `HttpClient` for `https://graph.microsoft.com/` and send to `v1.0/copilot/retrieval`.
- Request a token once per retrieval operation and apply it as a Bearer token only to that request.
- Translate non-success responses into one `Microsoft365RetrievalException` type.
- Expose `HttpStatusCode? StatusCode` and `string? RequestId`.
- Prefer `request-id`, then `client-request-id`.
- Use stable actionable message categories for `400`, `401`, `403`, `429`, and `5xx`; do not read a non-success response body merely to construct an error.
- Wrap `HttpRequestException` transport failures with a null status unless the original exception supplies one, and preserve the inner exception.
- Wrap malformed-success failures with the successful HTTP status and preserve the inner serialization exception.
- Let token-provider exceptions and `OperationCanceledException` propagate unchanged so hosts retain authentication and cancellation semantics.

### 7. Logging

Use `ILogger<Microsoft365RetrievalClient>` with these internal event IDs and levels:

| ID | Name | Level | Emitted when |
| --- | --- | --- | --- |
| 1000 | `RetrievalStarted` | Information | A locally valid operation starts, before token acquisition |
| 1001 | `RetrievalCompleted` | Information | A successful response has been mapped |
| 1002 | `RetrievalFailed` | Warning | A non-429 HTTP failure, transport failure, or malformed successful payload occurs |
| 1003 | `RetrievalThrottled` | Warning | A 429 response occurs; do not also emit `RetrievalFailed` |

Cancellation emits no failure or completion event. Token-provider failure propagates unchanged and emits `RetrievalFailed` without logging the provider exception object or message.

Information logs may contain elapsed time, status, result count, configured maximum, and whether a filter is present. They must not contain the token, authorization header, query text, filter contents, extracts, metadata values, or raw bodies.

### 8. Dependency Injection and Package Dependencies

Provide:

```csharp
IServiceCollection AddMicrosoft365Retrieval(
    this IServiceCollection services,
    Action<Microsoft365RetrievalOptions> configure);
```

The method registers options validation, the typed `HttpClient`, and `IMicrosoft365RetrievalClient`. It does not register a token provider, authentication, retries, or Feature 002 services.

Add centrally managed direct references only for `Microsoft.Extensions.*` packages whose APIs Feature 001 compiles against. Start with `Microsoft.Extensions.Http` and the minimum options/logging abstractions proven necessary by compilation; pin stable `10.0.11` packages consistently with the resolved .NET 10 graph. Do not update the already-current Agent Framework or Identity Web versions in this feature.

## Dependency Graph

```text
Task 1: options and core interfaces
    |
    +--> Task 2: immutable results and exception contract
             |
             +--> Task 3: request validation and HTTP happy path
                      |
                      +--> Task 4: response deserialization and mapping
                               |
                               +--> Task 5: HTTP failures and cancellation
                                        |
                                        +--> Task 6: safe logging
                                                 |
                                                 +--> Task 7: DI and package dependencies
                                                          |
                                                          +--> Task 8: docs and closure
```

The sequence is intentionally strict because later tasks exercise and stabilize contracts introduced earlier. Documentation can be drafted independently after Task 2, but it should merge only after Tasks 3 through 7 settle observable behavior.

## API-Surface Gate

The existing test project is the Feature 001 API-surface test; no additional consumer project is required. `PublicContractTests.cs` MUST compile direct consumer-style declarations and calls against the project reference for every required public type and member. Reflection assertions cover negative shape requirements that compilation cannot prove, such as absent setters or overloads. The concrete gate is:

```powershell
dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*PublicContractTests"
```

## Task Plan

## Task 1: Define Options and Core Interfaces

**Description:** Establish the smallest public configuration and operation boundary before transport code exists. Use TDD to pin defaults, query/client signatures, nullability, and XML documentation.

**Acceptance criteria:**

- [ ] `Microsoft365RetrievalOptions` defaults to 8 results, no filter, and metadata `title` plus `author`.
- [ ] Token-provider and retrieval-client signatures exactly match the feature specification and accept optional cancellation tokens.
- [ ] Public API tests prevent accidental setters, overloads, or Agent Framework/ASP.NET coupling, and public members include XML documentation.

**Verification:**

- [ ] RED then GREEN: `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*PublicContractTests"`
- [ ] Build: `dotnet build Acterion.Agents.AI.slnx --configuration Release`

**Dependencies:** None

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalOptions.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/IMicrosoft365RetrievalTokenProvider.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/IMicrosoft365RetrievalClient.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/PublicContractTests.cs`

**Estimated scope:** S, 4 files

## Task 2: Define Immutable Results and Exception Contract

**Description:** Add the Feature 001/002 result boundary and the single package exception before implementing wire mapping. Pin scalar metadata preservation and exception inspection through consumer-facing tests.

**Acceptance criteria:**

- [ ] Hits and extracts are sealed, read-only to consumers, preserve extract order, and expose the specified nullable fields.
- [ ] Metadata uses a read-only ordinal dictionary of cloned `JsonElement` scalar values.
- [ ] The package exception exposes nullable status and request ID, supports an inner exception, and can be constructed without response content.

**Verification:**

- [ ] RED then GREEN: focused `*ResultContractTests` and `*PublicContractTests` classes.
- [ ] Build: `dotnet build Acterion.Agents.AI.slnx --configuration Release`

**Dependencies:** Task 1

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalHit.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalExtract.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalException.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/ResultContractTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/PublicContractTests.cs`

**Estimated scope:** M, 5 files

## Checkpoint: Public Contract

- [ ] Tasks 1 and 2 focused tests pass.
- [ ] The solution builds in Release.
- [ ] A separate consumer/API-surface test can compile every specified public contract.
- [ ] Human review confirms the `JsonElement` metadata contract before transport implementation continues.

## Task 3: Implement Query Validation and the HTTP Happy Path

**Description:** Implement the smallest end-to-end successful operation: validate the query, acquire one delegated token, serialize the exact SharePoint request, send it with Bearer authentication, and return an empty result for an empty hit array.

**Acceptance criteria:**

- [ ] Invalid queries fail before token acquisition and HTTP I/O, including the 1,500/1,501 boundary.
- [ ] A valid request uses POST, the exact v1.0 endpoint, Bearer auth, UTF-8 JSON, fixed `sharePoint`, configured options, and omission of a null filter.
- [ ] An empty `retrievalHits` array returns an empty read-only result without retries.

**Verification:**

- [ ] RED then GREEN: `*Microsoft365RetrievalClientRequestTests`.
- [ ] Build: `dotnet build Acterion.Agents.AI.slnx --configuration Release`.

**Dependencies:** Tasks 1 and 2

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalClient.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Internal/RetrievalWireModels.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Microsoft365RetrievalClientRequestTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/RecordingHttpMessageHandler.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/StubTokenProvider.cs`

**Estimated scope:** M, 5 files

## Task 4: Map Successful Retrieval Responses

**Description:** Complete successful response deserialization and immutable result mapping, treating the Graph payload as untrusted while tolerating unknown fields.

**Acceptance criteria:**

- [ ] Multiple hits and extracts preserve response order, optional relevance scores, original URLs, resource type, and scalar metadata types.
- [ ] Missing optional collections map to empty read-only collections and unknown properties are ignored.
- [ ] Missing required fields, malformed JSON, or object/array metadata values fail with a package exception preserving the inner serialization error.

**Verification:**

- [ ] RED then GREEN: `*Microsoft365RetrievalClientResponseTests`.
- [ ] Re-run `*Microsoft365RetrievalClientRequestTests` to prevent request regressions.
- [ ] Build: `dotnet build Acterion.Agents.AI.slnx --configuration Release`.

**Dependencies:** Task 3

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalClient.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Internal/RetrievalWireModels.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalHit.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Microsoft365RetrievalClientResponseTests.cs`

**Estimated scope:** M, 4 files

## Checkpoint: Successful Retrieval

- [ ] Request and response focused tests pass together.
- [ ] No test opens a network connection or requires credentials.
- [ ] The solution remains buildable in Release.
- [ ] The happy path covers the complete Feature 001 client operation without Feature 002 mapping.

## Task 5: Implement Failure Translation and Cancellation

**Description:** Add deterministic status handling, request-ID extraction, transport failure wrapping, and cancellation propagation without reading sensitive failure bodies or retrying.

**Acceptance criteria:**

- [ ] `400`, `401`, `403`, `429`, and representative `5xx` responses produce the single documented exception with status and preferred request ID; transport failures are wrapped with status and inner-exception semantics defined above.
- [ ] Messages are actionable by category and contain no token, query, filter, authorization header, or raw body.
- [ ] Token, send, and deserialization cancellation propagate as `OperationCanceledException`; token-provider failures propagate unchanged; no request is retried.

**Verification:**

- [ ] RED then GREEN: focused `*Microsoft365RetrievalClientFailureTests` and `*Microsoft365RetrievalClientCancellationTests`.
- [ ] Build: `dotnet build Acterion.Agents.AI.slnx --configuration Release`.

**Dependencies:** Task 4

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalClient.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalException.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Microsoft365RetrievalClientFailureTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Microsoft365RetrievalClientCancellationTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/RecordingHttpMessageHandler.cs`

**Estimated scope:** M, 5 files

## Task 6: Add Safe Structured Logging

**Description:** Instrument observable client outcomes with stable event IDs and a deliberately small, non-sensitive property set.

**Acceptance criteria:**

- [ ] Started, completed, failed, and throttled outcomes emit exactly IDs 1000 through 1003 with the levels and exclusivity rules defined above.
- [ ] Default logs never contain token values, auth headers, queries, filters, extracts, metadata values, or raw bodies.
- [ ] Logging does not change exception, cancellation, or HTTP behavior.

**Verification:**

- [ ] RED then GREEN: `*Microsoft365RetrievalClientLoggingTests`.
- [ ] Re-run failure and cancellation focused tests.
- [ ] Build: `dotnet build Acterion.Agents.AI.slnx --configuration Release`.

**Dependencies:** Task 5

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalClient.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Internal/RetrievalLogEvents.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Microsoft365RetrievalClientLoggingTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/CollectingLogger.cs`

**Estimated scope:** M, 4 files

## Checkpoint: Client Behavior

- [ ] Tasks 3 through 6 focused tests pass as one client test slice.
- [ ] Security assertions inspect exception messages and captured logs.
- [ ] There is no retry, cache, Graph SDK, Identity Web implementation, or Agent Framework mapping.
- [ ] The solution builds cleanly in Release.

## Task 7: Add DI Registration and Options Validation

**Description:** Register the typed client and options through a thin service-collection extension, making invalid configuration and a missing token provider fail deterministically while declaring only the direct package dependencies actually compiled against.

**Acceptance criteria:**

- [ ] Registration configures the Graph base address, options, and `IMicrosoft365RetrievalClient` without replacing unrelated host services.
- [ ] Invalid result count or metadata configuration fails when the registered client/options graph is resolved; a missing token provider is not silently substituted.
- [ ] Required `Microsoft.Extensions.*` references are centrally pinned, direct, stable, and no retry/Graph dependency is added.

**Verification:**

- [ ] RED then GREEN: `*DependencyInjectionTests` and `*Microsoft365RetrievalOptionsTests`.
- [ ] Inspect: `dotnet list src/Acterion.Agents.AI.Microsoft365.Retrieval/Acterion.Agents.AI.Microsoft365.Retrieval.csproj package --include-transitive`.
- [ ] Build: `dotnet build Acterion.Agents.AI.slnx --configuration Release`.

**Dependencies:** Task 6

**Files likely touched:**

- `Directory.Packages.props`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Acterion.Agents.AI.Microsoft365.Retrieval.csproj`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalServiceCollectionExtensions.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/DependencyInjectionTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Microsoft365RetrievalOptionsTests.cs`

**Estimated scope:** M, 5 files

## Task 8: Record Evidence and Close Feature 001

**Description:** Execute the complete Feature 001 gate and record reproducible evidence without taking ownership of root documentation, package inspection, or release packaging.

**Acceptance criteria:**

- [ ] `PublicContractTests` compile consumer-style use of every required public member and verify prohibited surface by reflection.
- [ ] Every Feature 001 acceptance criterion and Plan Gate item has a row in `implementation-evidence.md` containing the command or focused test, observed result, and implementation commit/worktree reference.
- [ ] The evidence record states that retries were deferred and confirms public XML documentation was generated by the Release build.

**Verification:**

- [ ] Full tests: `dotnet test --solution Acterion.Agents.AI.slnx --configuration Release`.
- [ ] Full build: `dotnet build Acterion.Agents.AI.slnx --configuration Release`.
- [ ] Focused API surface: `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*PublicContractTests"`.

**Dependencies:** Task 7

**Files likely touched:**

- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/PublicContractTests.cs`
- `specs/features/001-sharepoint-retrieval-client/implementation-evidence.md`

**Estimated scope:** S, 2 files

## Checkpoint: Feature Complete

- [ ] All Feature 001 tests pass on Microsoft Testing Platform.
- [ ] Release build succeeds and emits XML documentation.
- [ ] All specification acceptance criteria have evidence.
- [ ] No normal verification requires tenant credentials, network access, or a Microsoft 365 license.
- [ ] Human review approves Feature 001 before Features 002, 003, 004, or 005 depend on its public contract.

## Acceptance-Criteria Coverage

| Feature criterion | Planned evidence |
| --- | --- |
| Valid v1.0 SharePoint request with delegated Bearer token | Tasks 3 and 7 request/DI tests |
| Invalid input fails before token or HTTP | Tasks 3 and 7 validation tests |
| Typed hits and empty result behavior | Tasks 2 and 4 result/response tests |
| Cancellation and Graph failures are safe | Task 5 failure/cancellation tests |
| Logs and exceptions contain no sensitive data | Tasks 5 and 6 security assertions |
| Focused tests and Release build pass | Every checkpoint and Task 8 full gate |
| Public contract compiles through an API-surface test | Tasks 1, 2, and 8 `PublicContractTests` gate |
| Retry decision recorded | Deferred explicitly in this plan |
| Serialization and MTP command proven | Tasks 3, 4, and 8 |

## Risks and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Public `JsonElement` metadata becomes hard to change | High | Lock it with consumer tests and require human approval at the first checkpoint |
| `JsonElement` values outlive serializer-owned state | High | Clone every exposed value and test access after deserialization completes |
| Invalid KQL succeeds without intended scope | High | Never parse or advertise filtering as authorization; never derive it from the query |
| Sensitive content leaks through diagnostics | High | Do not read failure bodies for messages; use allowlisted log dimensions and negative assertions |
| Cancellation is accidentally wrapped | Medium | Dedicated cancellation tests at token, send, and deserialize boundaries |
| DI works only because of transitive packages | Medium | Add centrally pinned direct references for APIs compiled against and inspect the package graph |
| Options validation runs too late | Medium | Resolve the registered graph in tests and assert validation failure before any request |
| Test helper complexity hides behavior | Medium | Use small fakes and DAMP tests; avoid mocking frameworks and shared fixture hierarchies |
| Optional retry support expands scope | Medium | Defer retries completely in Feature 001 and document the decision |

## Open Questions for Human Review

These are approval points for the plan, not implementation choices to rediscover during coding:

1. Approve `IReadOnlyDictionary<string, JsonElement>` as the public metadata contract, with object/array metadata treated as malformed.
2. Approve wrapping malformed successful responses and `HttpRequestException` transport failures in `Microsoft365RetrievalException`; transport status is null unless supplied by the original exception, while token-provider and cancellation exceptions propagate unchanged.
3. Approve deferring all automatic retries from v0.1 Feature 001.

## Implementation Evidence Format

Task 8 creates `implementation-evidence.md` with one row per Feature 001 acceptance criterion and Plan Gate item:

| Requirement | Test or command | Observed result | Commit/worktree reference |
| --- | --- | --- | --- |
| Example: invalid query fails before token acquisition | `*Microsoft365RetrievalClientRequestTests` | Pass, date and relevant assertion | Commit hash or `uncommitted` |

The record MUST include the full MTP command, Release build command, focused serialization and API-surface tests, inspected direct package graph, and the explicit no-retry decision. RED observations are recorded in the applicable task row or implementation notes; they are workflow evidence, not substitutes for executable GREEN verification.

## Task-Level Definition of Done

A task counts as complete only when:

- its focused test was observed failing before implementation and passing afterward;
- all acceptance criteria for that task are checked with evidence;
- its documented build command succeeds;
- no task touches more than five files without being split and re-reviewed;
- no credentials, live endpoints beyond the fixed Graph URI, or tenant data are introduced;
- public APIs include XML documentation and nullable annotations;
- unrelated specs, features, and scaffold files remain unchanged;
- the corresponding item in [`todo.md`](todo.md) is updated immediately.

## Authoritative Sources

- Retrieval API request, filter properties, and examples: https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/copilotroot-retrieval
- Retrieval API limitations and permission trimming: https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/overview
- SharePoint KQL syntax: https://learn.microsoft.com/en-us/sharepoint/dev/general-development/keyword-query-language-kql-syntax-reference
- `System.Text.Json` overview: https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview
- .NET HTTP client factory guidance: https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory
- .NET options validation: https://learn.microsoft.com/en-us/dotnet/core/extensions/options
