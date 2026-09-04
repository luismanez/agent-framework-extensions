# Task Checklist: Feature 001 - SharePoint Retrieval Client

**Specification:** [`001-sharepoint-retrieval-client.md`](001-sharepoint-retrieval-client.md)
**Plan:** [`plan.md`](plan.md)
**Status:** Task 4 complete; Task 5 is next

Update this file as each RED-GREEN-REFACTOR cycle completes. A checked task must satisfy every acceptance and verification item below.

## Phase 1: Public Contract

## Task 1: Define Options and Core Interfaces

**Description:** Establish public configuration, token acquisition, and retrieval operation contracts before transport implementation.

**Acceptance criteria:**

- [x] Options default to 8 results, no filter, and `title` plus `author` metadata.
- [x] Token-provider and retrieval-client signatures match the feature specification.
- [x] Public contracts accept cancellation, do not couple to Agent Framework or ASP.NET Core, expose no accidental setters/overloads, and include XML documentation.

**Verification:**

- [x] RED observed for `*PublicContractTests` before implementation.
- [x] Focused tests pass:
  `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*PublicContractTests"`
- [x] Release build passes:
  `dotnet build Acterion.Agents.AI.slnx --configuration Release`

**Dependencies:** None
**Estimated scope:** S, 4 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Microsoft365RetrievalOptions.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Authentication/IMicrosoft365RetrievalTokenProvider.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/IMicrosoft365RetrievalClient.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/PublicContract/PublicContractTests.cs`

## Task 2: Define Immutable Results and Exception Contract

**Description:** Establish the immutable result/extract boundary and the single inspectable package exception.

**Acceptance criteria:**

- [x] Hit and extract types are sealed and read-only to consumers.
- [x] Metadata is exposed as an ordinal read-only dictionary of cloned scalar `JsonElement` values.
- [x] The package exception exposes nullable status and request ID, preserves an inner exception, and can be constructed without response content.

**Verification:**

- [x] RED observed for `*ResultContractTests` before implementation.
- [x] Focused `*ResultContractTests` and `*PublicContractTests` pass.
- [x] Release build passes.

**Dependencies:** Task 1
**Estimated scope:** M, 5 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Models/Microsoft365RetrievalHit.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Models/Microsoft365RetrievalExtract.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Microsoft365RetrievalException.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/Models/ResultContractTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/PublicContract/PublicContractTests.cs`

## Checkpoint: Public Contract

- [x] Tasks 1 and 2 focused tests pass together.
- [x] Release build succeeds.
- [x] Consumer/API-surface test compiles all specified public contracts.
- [x] Human approves the public `JsonElement` metadata contract.

## Phase 2: Retrieval Behavior

## Task 3: Implement Query Validation and HTTP Happy Path

**Description:** Validate input, acquire a delegated token, serialize the request, send it once, and handle an empty successful response.

**Acceptance criteria:**

- [x] Invalid queries fail before token acquisition and HTTP I/O.
- [x] Valid calls use the exact v1.0 endpoint, POST, Bearer auth, UTF-8 JSON, `sharePoint`, and configured options.
- [x] Empty hits return an empty read-only result and no retry occurs.

**Verification:**

- [x] RED observed for `*Microsoft365RetrievalClientRequestTests` because `Microsoft365RetrievalClient` did not exist.
- [x] Focused request tests pass: 7 passed, 0 failed, 0 skipped.
- [x] Release build and the full tenant-independent test suite pass.

**Dependencies:** Tasks 1 and 2
**Estimated scope:** M, 5 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Microsoft365RetrievalClient.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Internal/RetrievalWireModels.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/Microsoft365RetrievalClientRequestTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/RecordingHttpMessageHandler.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/StubTokenProvider.cs`

## Task 4: Map Successful Retrieval Responses

**Description:** Deserialize successful Graph payloads and map them to immutable public results.

**Acceptance criteria:**

- [x] Hit/extract order, URLs, relevance, resource type, and scalar metadata types are preserved.
- [x] Missing optional collections become empty and unknown fields are ignored.
- [x] Missing required fields, malformed JSON, or nonscalar metadata produce a package exception preserving the inner serialization error.

**Verification:**

- [x] RED observed for `*Microsoft365RetrievalClientResponseTests` because non-empty `retrievalHits` were rejected.
- [x] Focused response tests pass: 8 passed, 0 failed, 0 skipped; request tests remain 7 passed, 0 failed, 0 skipped.
- [x] Release build passes.

**Dependencies:** Task 3
**Estimated scope:** M, 4 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Microsoft365RetrievalClient.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Internal/RetrievalWireModels.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Models/Microsoft365RetrievalHit.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/Microsoft365RetrievalClientResponseTests.cs`

## Checkpoint: Successful Retrieval

- [x] Request and response focused tests pass.
- [ ] No test opens a network connection or requires credentials.
- [x] Release build succeeds.
- [x] Happy path remains independent of Feature 002 mapping.

## Task 5: Implement Failure Translation and Cancellation

**Description:** Translate HTTP/transport failures and preserve cancellation and token-provider semantics.

**Acceptance criteria:**

- [ ] `400`, `401`, `403`, `429`, and `5xx` use one exception with status and preferred request ID; `HttpRequestException` is wrapped with null status unless it supplies one and its inner exception is preserved.
- [ ] Error messages contain no token, query, filter, auth header, or response body.
- [ ] Cancellation and token-provider failures propagate unchanged and no request retries.

**Verification:**

- [ ] RED observed for failure and cancellation tests.
- [ ] Focused `*Microsoft365RetrievalClientFailureTests` and `*Microsoft365RetrievalClientCancellationTests` pass.
- [ ] Release build passes.

**Dependencies:** Task 4
**Estimated scope:** M, 5 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Microsoft365RetrievalClient.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Microsoft365RetrievalException.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/Microsoft365RetrievalClientFailureTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/Microsoft365RetrievalClientCancellationTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/RecordingHttpMessageHandler.cs`

## Task 6: Add Safe Structured Logging

**Description:** Add allowlisted operational logs without exposing sensitive input or content.

**Acceptance criteria:**

- [ ] Logging uses `RetrievalStarted` 1000/Information, `RetrievalCompleted` 1001/Information, `RetrievalFailed` 1002/Warning, and `RetrievalThrottled` 1003/Warning.
- [ ] A 429 emits throttled but not failed; cancellation emits neither failure nor completion.
- [ ] Captured logs contain only approved dimensions.
- [ ] Logging does not alter error, cancellation, or request behavior.

**Verification:**

- [ ] RED observed for `*Microsoft365RetrievalClientLoggingTests`.
- [ ] Logging, failure, and cancellation focused tests pass.
- [ ] Release build passes.

**Dependencies:** Task 5
**Estimated scope:** M, 4 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Microsoft365RetrievalClient.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Internal/RetrievalLogEvents.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/Microsoft365RetrievalClientLoggingTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/CollectingLogger.cs`

## Checkpoint: Client Behavior

- [ ] Tasks 3 through 6 focused tests pass together.
- [ ] Security assertions cover exception messages and logs.
- [ ] No retry, cache, Graph SDK, Identity Web implementation, or Agent Framework mapping exists.
- [ ] Release build succeeds.

## Phase 3: Host Integration and Closure

## Task 7: Add DI Registration and Options Validation

**Description:** Register options and the typed client while declaring only the direct dependencies Feature 001 compiles against.

**Acceptance criteria:**

- [ ] Registration configures Graph base address, options, and `IMicrosoft365RetrievalClient` without replacing host services.
- [ ] Invalid options and missing token provider fail deterministically when the service graph is resolved.
- [ ] Required `Microsoft.Extensions.*` packages are direct, centrally pinned stable dependencies and no retry/Graph package is added.

**Verification:**

- [ ] RED observed for `*DependencyInjectionTests` and `*Microsoft365RetrievalOptionsTests`.
- [ ] Focused DI and options tests pass.
- [ ] Dependency graph inspected with:
  `dotnet list src/Acterion.Agents.AI.Microsoft365.Retrieval/Acterion.Agents.AI.Microsoft365.Retrieval.csproj package --include-transitive`
- [ ] Release build passes.

**Dependencies:** Task 6
**Estimated scope:** M, 5 files

**Files likely touched:**

- `Directory.Packages.props`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Acterion.Agents.AI.Microsoft365.Retrieval.csproj`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Microsoft365RetrievalServiceCollectionExtensions.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/DependencyInjectionTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/Microsoft365RetrievalOptionsTests.cs`

## Task 8: Record Evidence and Close Feature 001

**Description:** Run the complete Feature 001 gate and record reproducible evidence without taking ownership of release documentation or packaging.

**Acceptance criteria:**

- [ ] `PublicContractTests` compile consumer-style use of every required public member and verify prohibited surface by reflection.
- [ ] Every acceptance criterion and Plan Gate item is recorded with command/test, result, and commit/worktree reference.
- [ ] Evidence states that retries are deferred and confirms Release XML documentation output.

**Verification:**

- [ ] Full tests pass:
  `dotnet test --solution Acterion.Agents.AI.slnx --configuration Release`
- [ ] Full build passes:
  `dotnet build Acterion.Agents.AI.slnx --configuration Release`
- [ ] API-surface gate passes:
  `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*PublicContractTests"`

**Dependencies:** Task 7
**Estimated scope:** S, 3 files

**Files likely touched:**

- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/PublicContract/PublicContractTests.cs`
- `specs/features/001-sharepoint-retrieval-client/implementation-evidence.md`

## Checkpoint: Feature Complete

- [ ] All Feature 001 tests pass on Microsoft Testing Platform.
- [ ] Release build succeeds and emits XML documentation.
- [ ] Every Feature 001 acceptance criterion has recorded evidence.
- [ ] Normal verification requires no tenant credentials, network access, or Microsoft 365 license.
- [ ] Human review approves Feature 001 before dependent features consume its public contract.

## Plan Approval

- [x] Approve `IReadOnlyDictionary<string, JsonElement>` for public metadata; reject object/array metadata as malformed.
- [ ] Approve wrapping malformed successful responses and `HttpRequestException` transport failures; transport status is null unless supplied, while token-provider and cancellation failures propagate unchanged.
- [ ] Approve deferring automatic retries from Feature 001 v0.1.
- [ ] Approve this plan and authorize Task 1 implementation.

## Acceptance-Criteria Traceability

- [ ] Valid delegated v1.0 request: Tasks 3 and 7.
- [ ] Invalid input before token/HTTP: Tasks 3 and 7.
- [ ] Typed and empty results: Tasks 2 and 4.
- [ ] Cancellation and Graph failures: Task 5.
- [ ] Safe logs and exceptions: Tasks 5 and 6.
- [ ] API-surface, serialization, dependency graph, MTP, and Release evidence: Tasks 7 and 8.
- [ ] Explicit no-retry decision: Plan approval and Task 8 evidence.
