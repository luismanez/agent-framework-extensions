# Task Checklist: Feature 002 - Agent Framework Integration

**Specification:** [`002-agent-framework-integration.md`](002-agent-framework-integration.md)
**Plan:** [`plan.md`](plan.md)
**Status:** Awaiting plan approval

Update this file after each RED-GREEN-REFACTOR cycle. A checked task must satisfy every acceptance and verification item below.

## Phase 1: Contract and Mapping

## Task 1: Pin Adapter and Builder Contracts

**Description:** Compile consumer-style tests for the exact adapter method and both builder overloads against centrally pinned Agent Framework APIs before production code is added.

**Acceptance criteria:**

- [ ] `Microsoft365RetrievalSearch.SearchAsync` is assignable to the required `TextSearchProvider` delegate.
- [ ] Both specified `UseMicrosoft365Retrieval` overloads compile and no prohibited convenience API exists.
- [ ] The probe exercises `RawRepresentation`, both `TextSearchBehavior` values, deferred builder middleware, and public XML documentation.

**Verification:**

- [ ] RED observed for `*AgentFrameworkPublicContractTests`.
- [ ] Focused tests pass: `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*AgentFrameworkPublicContractTests"`
- [ ] Release build passes: `dotnet build Acterion.Agents.AI.slnx --configuration Release`

**Dependencies:** Approved Feature 001 public contract
**Estimated scope:** S, 3 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalSearch.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalAgentBuilderExtensions.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/AgentFrameworkPublicContractTests.cs`

## Task 2: Implement Deterministic Result Mapping

**Description:** Forward retrieval calls and map each Feature 001 hit into one framework search result without ranking, filtering, or content promotion.

**Acceptance criteria:**

- [ ] Query and cancellation are forwarded unchanged, hit order is preserved, and empty hits return an empty sequence.
- [ ] Title, decoded URI-segment, and original-URL source-name paths are deterministic; source links remain unchanged.
- [ ] Non-whitespace extracts join with one newline, empty extracts produce empty text, raw hit identity is retained, and sensitivity data is absent from visible fields.

**Verification:**

- [ ] RED observed for `*Microsoft365RetrievalSearchTests`.
- [ ] Focused tests pass: `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*Microsoft365RetrievalSearchTests"`
- [ ] Task 1 contract tests and Release build pass.

**Dependencies:** Task 1
**Estimated scope:** M, 3 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalSearch.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Microsoft365RetrievalSearchTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/StubRetrievalClient.cs`

## Checkpoint: Adapter Contract

- [ ] Tasks 1 and 2 focused tests pass together.
- [ ] Release build succeeds.
- [ ] Human confirms empty-extract and source-name fallback behavior.
- [ ] No custom RAG, tool, prompt, memory, ranking, or citation type exists.

## Phase 2: Builder and Runtime Behavior

## Task 3: Add Deferred Builder Composition

**Description:** Implement both builder overloads using Agent Framework middleware and resolve the adapter only from the service provider supplied to `Build`.

**Acceptance criteria:**

- [ ] The short overload sets only the explicit behavior and delegates to the options overload.
- [ ] The options overload resolves the adapter at build time, preserves the same options instance, and creates one provider per built agent.
- [ ] Null arguments fail synchronously and missing retrieval registration fails clearly during `Build`.

**Verification:**

- [ ] RED observed for `*Microsoft365RetrievalAgentBuilderExtensionsTests`.
- [ ] Focused builder tests pass.
- [ ] Mapping and public-contract tests and Release build pass.

**Dependencies:** Task 2
**Estimated scope:** M, 4 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalAgentBuilderExtensions.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalServiceCollectionExtensions.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Microsoft365RetrievalAgentBuilderExtensionsTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/RecordingAgent.cs`

## Task 4: Prove Automatic Retrieval Behavior

**Description:** Exercise the real `TextSearchProvider` in `BeforeAIInvoke` mode against an in-memory retrieval client and recording model boundary.

**Acceptance criteria:**

- [ ] Search executes before the underlying fake agent.
- [ ] Framework-formatted retrieval context containing source name, link, and text reaches the model invocation.
- [ ] Cancellation reaches retrieval and no retrieved content is promoted to application or system policy.

**Verification:**

- [ ] RED observed for the automatic-mode cases in `*Microsoft365RetrievalAgentBehaviorTests`.
- [ ] Focused behavior tests pass.
- [ ] Tasks 1 through 3 tests and Release build pass.

**Dependencies:** Task 3
**Estimated scope:** M, 3 files

**Files likely touched:**

- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Microsoft365RetrievalAgentBehaviorTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/RecordingAgent.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/StubRetrievalClient.cs`

## Task 5: Prove On-Demand and Pipeline Behavior

**Description:** Verify that the real provider advertises and executes its own search function while coexisting with another pipeline stage or context provider.

**Acceptance criteria:**

- [ ] On-demand mode performs no eager retrieval and advertises the Agent Framework search function.
- [ ] Invoking the advertised function reaches the fake retrieval client with its query and cancellation token.
- [ ] Existing pipeline behavior remains active, repeated builds do not share providers, and absent services produce actionable build-time failure.

**Verification:**

- [ ] RED observed for the on-demand and composition cases in the behavior and builder test classes.
- [ ] Focused `*Microsoft365RetrievalAgentBehaviorTests` and `*Microsoft365RetrievalAgentBuilderExtensionsTests` pass.
- [ ] Full package tests and Release build pass.

**Dependencies:** Task 4
**Estimated scope:** M, 4 files

**Files likely touched:**

- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Microsoft365RetrievalAgentBehaviorTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Microsoft365RetrievalAgentBuilderExtensionsTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/RecordingAgent.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/RecordingPipelineAgent.cs`

## Checkpoint: Framework Integration

- [ ] Both real `TextSearchProvider` modes pass through public behavior tests.
- [ ] Builder composition and two-build isolation tests pass.
- [ ] All tests remain tenant-, credential-, model-, and network-independent.
- [ ] Release build succeeds.

## Phase 3: Closure

## Task 6: Record Evidence and Close Feature 002

**Description:** Run the complete feature gate and record reproducible evidence for every specification criterion and Plan Gate item.

**Acceptance criteria:**

- [ ] Evidence maps every acceptance criterion to a focused test or command and records the observed result and commit/worktree reference.
- [ ] Public XML documentation describes untrusted retrieved content and both overloads without duplicating framework documentation.
- [ ] Evidence confirms no prohibited convenience type, custom tool, live dependency, or sensitivity-data projection was added.

**Verification:**

- [ ] Full tests pass: `dotnet test --solution Acterion.Agents.AI.slnx --configuration Release`
- [ ] Release build passes: `dotnet build Acterion.Agents.AI.slnx --configuration Release`
- [ ] Focused mapping, public-contract, builder, and behavior classes pass together.

**Dependencies:** Task 5
**Estimated scope:** S, 3 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalSearch.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalAgentBuilderExtensions.cs`
- `specs/features/002-agent-framework-integration/implementation-evidence.md`

## Checkpoint: Feature Complete

- [ ] Every Feature 002 acceptance criterion has recorded evidence.
- [ ] Full tests and Release build pass on Microsoft Testing Platform.
- [ ] Public API remains exactly the specified adapter, method, and two builder overloads.
- [ ] Human review approves Feature 002 before Feature 004 consumes it.

## Plan Approval

- [ ] Approve empty `Text` for hits without usable extracts.
- [ ] Approve build-time adapter resolution and one provider per built agent.
- [ ] Approve the real advertised function as the on-demand test boundary.
- [ ] Approve this plan and authorize Task 1 implementation.