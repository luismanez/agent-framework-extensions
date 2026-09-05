# Task Checklist: Feature 005 - Typed SharePoint Retrieval Filters

**Specification:** [`005-typed-sharepoint-retrieval-filters.md`](005-typed-sharepoint-retrieval-filters.md)
**Plan:** [`plan.md`](plan.md)
**Status:** Implementation complete; awaiting human sign-off

Update this file after each RED-GREEN-REFACTOR cycle. A checked task must satisfy every acceptance and verification item below.

## Phase 1: Contract and Primitive Terms

## Task 1: Lock the Public Filter Contract

**Description:** Add consumer-style and reflection tests for the exact immutable `SharePointRetrievalFilter` API, then introduce the smallest private-construction value type that satisfies that surface.

**Acceptance criteria:**

- [x] The sealed type exposes only read-only `Expression` and static `Path(Uri)`, `SiteId(Guid)`, and `AnyOf(params SharePointRetrievalFilter[])` factories.
- [x] No public constructor, setter, implicit conversion, raw-string factory, interface, options type, or extension method exists.
- [x] The type and every public member have XML documentation that describes retrieval filtering without authorization claims.

**Verification:**

- [x] RED observed for `*SharePointRetrievalFilterPublicContractTests` before the type exists.
- [x] Focused public-contract tests pass: `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*SharePointRetrievalFilterPublicContractTests"`
- [x] Release build passes: `dotnet build Acterion.Agents.AI.slnx --configuration Release`

**Dependencies:** Approved Feature 001 options contract
**Estimated scope:** S, 2 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Filtering/SharePointRetrievalFilter.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/PublicContract/SharePointRetrievalFilterPublicContractTests.cs`

## Task 2: Implement Path and Site-ID Terms

**Description:** Validate typed URI and GUID inputs and emit exact canonical KQL terms using only structured BCL APIs.

**Acceptance criteria:**

- [x] Valid site, folder, and file HTTPS URIs emit `Path:"{AbsoluteUri}"` while preserving escaped path and trailing-slash semantics.
- [x] Null, relative, non-HTTPS, user-info, query, and fragment URI inputs fail synchronously with the specified exception category; `Guid.Empty` fails and non-empty GUIDs emit lowercase invariant `SiteID:"..."`.
- [x] Unicode, `%22`, and KQL-looking URI segments remain escaped data and cannot create another term or operator.

**Verification:**

- [x] RED observed for primitive cases in `*SharePointRetrievalFilterTests`.
- [x] Focused filter tests pass: `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*SharePointRetrievalFilterTests"`
- [x] Public-contract tests and Release build pass.

**Dependencies:** Task 1
**Estimated scope:** M, 2 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Filtering/SharePointRetrievalFilter.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/Filtering/SharePointRetrievalFilterTests.cs`

## Checkpoint: Primitive Filters

- [x] Tasks 1 and 2 focused tests pass together.
- [x] Recorded .NET 10 `AbsoluteUri` outputs match the Plan Gate semantics.
- [x] No URI decoding, ad hoc parsing, host allowlist, or Graph lookup exists.
- [ ] Human confirms exact path and GUID output.

## Phase 2: Composition and Integration

## Task 3: Implement Immutable OR Composition

**Description:** Compose one or more typed filters by snapshotting and flattening their internal primitive terms without parsing, sorting, deduplicating, or mutating expressions.

**Acceptance criteria:**

- [x] A single filter keeps its expression unchanged; multiple path, site-ID, mixed, nested, ordered, and duplicate inputs emit one deterministic uppercase ` OR ` expression.
- [x] `AnyOf(A, AnyOf(B, C))` emits `(A OR B OR C)` and caller array mutation after construction cannot alter the result.
- [x] Null arrays use `ArgumentNullException`; empty arrays and null elements use `ArgumentException`, all synchronously.

**Verification:**

- [x] RED observed for composition cases in `*SharePointRetrievalFilterTests`.
- [x] Focused filter tests pass.
- [x] Public-contract tests and Release build pass.

**Dependencies:** Task 2
**Estimated scope:** S, 2 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Filtering/SharePointRetrievalFilter.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/Filtering/SharePointRetrievalFilterTests.cs`

## Task 4: Prove Feature 001 Integration and Guidance

**Description:** Verify direct assignment and unchanged request serialization, then document trusted-value usage, advanced raw filters, and the complete authorization boundary.

**Acceptance criteria:**

- [x] Assigning `filter.Expression` to `Microsoft365RetrievalOptions.FilterExpression` produces the same `filterExpression` JSON value in Feature 001's request and triggers no parallel request behavior.
- [x] Documentation shows path, site-ID, mixed, and nested examples and preserves raw `FilterExpression` for advanced KQL.
- [x] Guidance states that inputs are trusted application values, filtering is not authorization, Microsoft 365 permission trimming is authoritative, and hosts own endpoint and business authorization.

**Verification:**

- [x] RED observed for the Feature 001 serialization integration case before final wiring.
- [x] Focused filter and request-serialization tests pass together.
- [x] `git diff --check` passes for package guidance and Feature 005 documents.
- [x] Dependency inspection confirms no new package: `dotnet list src/Acterion.Agents.AI.Microsoft365.Retrieval/Acterion.Agents.AI.Microsoft365.Retrieval.csproj package --include-transitive`

**Dependencies:** Task 3 and implemented Feature 001 serialization
**Estimated scope:** M, 4 files

**Files likely touched:**

- `README.md`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Filtering/SharePointRetrievalFilter.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/Filtering/SharePointRetrievalFilterTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/Filtering/Microsoft365RetrievalRequestSerializationTests.cs`

## Checkpoint: Integrated Filter

- [x] Composition and Feature 001 serialization tests pass without credentials or network access.
- [x] Raw `FilterExpression` remains supported and unchanged.
- [x] Documentation never describes filters as scopes or authorization.
- [x] Release build succeeds with no new dependency or project.

## Phase 3: Closure

## Task 5: Record Evidence and Close Feature 005

**Description:** Run the complete feature gate and record reproducible evidence for the public contract, BCL URI observations, canonical expressions, security invariants, and unchanged dependency graph.

**Acceptance criteria:**

- [x] Evidence maps every acceptance criterion to a focused test or command with observed result and commit/worktree reference.
- [x] Evidence records representative `AbsoluteUri` outputs, exact `Path`, `SiteID`, and flattened `OR` forms, and unchanged Feature 001 serialization.
- [x] Evidence confirms no new dependency, project, DI registration, live test, authorization claim, length limit, or raw-expression factory.

**Verification:**

- [x] Focused tests pass: `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*SharePointRetrievalFilter*"`
- [x] Full tests pass: `dotnet test --solution Acterion.Agents.AI.slnx --configuration Release`
- [x] Release build passes: `dotnet build Acterion.Agents.AI.slnx --configuration Release`
- [x] Repository-wide `git diff --check` passes.

**Dependencies:** Task 4
**Estimated scope:** S, 2 files

**Files likely touched:**

- `README.md`
- `specs/features/005-typed-sharepoint-retrieval-filters/implementation-evidence.md`

## Checkpoint: Feature Complete

- [x] Every tool-verifiable Feature 005 acceptance criterion and Plan Gate item has recorded evidence.
- [x] Full tests and Release build pass on Microsoft Testing Platform.
- [x] Public API remains exactly the specified sealed type and four members.
- [ ] Human review approves Feature 005 for v0.1.

## Plan Approval

- [x] Approve `Uri.AbsoluteUri` as the path serialization boundary.
- [x] Approve URI component rejection and lowercase invariant `D` GUID formatting.
- [x] Approve nested `AnyOf` flattening with order and duplicates preserved.
- [x] Approve no length limit, dependency, project, DI registration, or raw-expression factory.
- [x] Approve this plan and authorize Task 1 implementation.