# Task Checklist: Feature 002 - Agent Framework Integration

**Specification:** [`002-agent-framework-integration.md`](002-agent-framework-integration.md)
**Plan:** [`plan.md`](plan.md)
**Status:** Both retrieval behaviors are implemented and covered by focused tests; closure verification remains.

Update this file after each RED-GREEN-REFACTOR cycle. A checked task must satisfy every acceptance and verification item below.

## Integration Boundary

`UseMicrosoft365Retrieval` decorates an `IChatClient` through `ChatClientBuilder.UseAIContextProviders`, which accepts the full `AIContextProvider` contract. For `OnDemandFunctionCalling`, it adds the native `UseFunctionInvocation` decorator after context enrichment. The caller creates its `ChatClientAgent` with `UseProvidedChatClientAsIs = true` for that mode, avoiding a second function invoker outside the provider.

## Phase 1: Contract and Mapping

## Task 1: Pin Adapter and Builder Contracts

**Description:** Compile consumer-style tests for the exact adapter method and both builder overloads against centrally pinned Agent Framework APIs before production code is added.

**Acceptance criteria:**

- [x] `Microsoft365RetrievalSearch.SearchAsync` is assignable to the required `TextSearchProvider` delegate.
- [x] Both specified `UseMicrosoft365Retrieval` overloads compile on `ChatClientBuilder` and no prohibited convenience API exists.
- [x] The probe exercises `RawRepresentation`, both `TextSearchBehavior` values, deferred builder middleware, and public XML documentation.

**Verification:**

- [ ] RED observed for `*AgentFrameworkPublicContractTests` (not recorded before the existing implementation).
- [x] Focused tests pass: `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*AgentFrameworkPublicContractTests"`
- [x] Release build passes: `dotnet build Acterion.Agents.AI.slnx --configuration Release`

**Dependencies:** Approved Feature 001 public contract
**Estimated scope:** S, 3 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/AgentFramework/Microsoft365RetrievalSearch.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/AgentFramework/Microsoft365RetrievalChatClientBuilderExtensions.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/PublicContract/AgentFrameworkPublicContractTests.cs`

## Task 2: Implement Deterministic Result Mapping

**Description:** Forward retrieval calls and map each Feature 001 hit into one framework search result without ranking, filtering, or content promotion.

**Acceptance criteria:**

- [x] Query and cancellation are forwarded unchanged, hit order is preserved, and empty hits return an empty sequence.
- [x] Title, decoded URI-segment, and original-URL source-name paths are deterministic; source links remain unchanged.
- [x] Non-whitespace extracts join with one newline, empty extracts produce empty text, raw hit identity is retained, and sensitivity data is absent from visible fields.

**Verification:**

- [ ] RED observed for `*Microsoft365RetrievalSearchTests` (not recorded before the existing implementation).
- [x] Focused tests pass: `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*Microsoft365RetrievalSearchTests"`
- [x] Task 1 contract tests and Release build pass.

**Dependencies:** Task 1
**Estimated scope:** M, 3 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/AgentFramework/Microsoft365RetrievalSearch.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/AgentFramework/Microsoft365RetrievalSearchTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/StubRetrievalClient.cs`

## Checkpoint: Adapter Contract

- [x] Tasks 1 and 2 focused tests pass together.
- [x] Release build succeeds.
- [ ] Human confirms empty-extract and source-name fallback behavior.
- [x] No custom RAG, tool, prompt, memory, ranking, or citation type exists.

## Phase 2: Builder and Runtime Behavior

## Task 3: Add Deferred Builder Composition

**Description:** Implement both chat-client builder overloads using Agent Framework middleware and resolve the adapter only from the service provider supplied to `Build`.

**Acceptance criteria:**

- [x] The short overload sets only the explicit behavior and delegates to the options overload.
- [x] The options overload resolves the adapter at build time, preserves the same options instance, and creates one provider per built chat-client pipeline.
- [x] Null arguments fail synchronously and missing retrieval registration fails clearly during `Build`.

**Verification:**

- [ ] RED observed for `*Microsoft365RetrievalChatClientBuilderExtensionsTests` (not recorded before the existing implementation).
- [x] Focused builder tests pass.
- [x] Mapping and public-contract tests and Release build pass.

**Dependencies:** Task 2
**Estimated scope:** M, 4 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/AgentFramework/Microsoft365RetrievalChatClientBuilderExtensions.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/Microsoft365RetrievalServiceCollectionExtensions.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/AgentFramework/Microsoft365RetrievalChatClientBuilderExtensionsTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/RecordingAgent.cs`

## Task 4: Prove Automatic Retrieval Behavior

**Description:** Exercise the real `TextSearchProvider` in `BeforeAIInvoke` mode against an in-memory retrieval client and recording model boundary.

**Acceptance criteria:**

- [x] Search executes before the underlying fake agent.
- [x] Framework-formatted retrieval context containing source name, link, and text reaches the model invocation.
- [x] Cancellation reaches retrieval and no retrieved content is promoted to application or system policy.

**Verification:**

- [ ] RED observed for the automatic-mode cases in `*Microsoft365RetrievalAgentBehaviorTests` (not recorded before the existing implementation).
- [x] Focused behavior tests pass.
- [x] Tasks 1 through 3 tests and Release build pass.

**Dependencies:** Task 3
**Estimated scope:** M, 3 files

**Files likely touched:**

- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/AgentFramework/Microsoft365RetrievalAgentBehaviorTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/RecordingAgent.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/StubRetrievalClient.cs`

## Task 5: Prove On-Demand and Pipeline Behavior

**Description:** Verify that the real provider advertises and executes its own search function while coexisting with another pipeline stage or context provider.

**Acceptance criteria:**

- [x] On-demand mode performs no eager retrieval and advertises the Agent Framework search function.
- [x] The native function-invocation decorator reaches the fake retrieval client and makes a second model call with the result.
- [x] Surrounding chat-client stages remain active, repeated builds do not share providers, and absent services produce actionable build-time failure.

**Verification:**

- [x] RED observed for the on-demand and composition cases in the behavior and builder test classes.
- [x] Focused `*Microsoft365RetrievalAgentBehaviorTests` and `*Microsoft365RetrievalChatClientBuilderExtensionsTests` pass.
- [x] Full package tests and Release build pass.

**Dependencies:** Task 4
**Estimated scope:** M, 4 files

**Files likely touched:**

- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/AgentFramework/Microsoft365RetrievalAgentBehaviorTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/AgentFramework/Microsoft365RetrievalChatClientBuilderExtensionsTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/RecordingAgent.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/RecordingPipelineAgent.cs`

## Checkpoint: Framework Integration

- [x] Both real `TextSearchProvider` modes pass through public behavior tests.
- [x] Builder composition and two-build isolation tests pass.
- [x] All tests remain tenant-, credential-, model-, and network-independent.
- [x] Release build succeeds.

## Phase 3: Closure

## Task 6: Record Evidence and Close Feature 002

**Description:** Run the complete feature gate and record reproducible evidence for every specification criterion and Plan Gate item.

**Acceptance criteria:**

- [x] Evidence maps every acceptance criterion to a focused test or command and records the observed result and commit/worktree reference, including the on-demand pipeline requirement.
- [x] Public XML documentation describes untrusted retrieved content and both overloads without duplicating framework documentation.
- [x] Evidence confirms no prohibited convenience type, custom tool, live dependency, or sensitivity-data projection was added.

**Verification:**

- [x] Full tests pass: `dotnet test --solution Acterion.Agents.AI.slnx --configuration Release` (59 passed).
- [x] Release build passes: `dotnet build Acterion.Agents.AI.slnx --configuration Release` (0 warnings, 0 errors).
- [x] Focused mapping, public-contract, builder, and behavior classes pass together.

**Dependencies:** Task 5
**Estimated scope:** S, 3 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/AgentFramework/Microsoft365RetrievalSearch.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/AgentFramework/Microsoft365RetrievalChatClientBuilderExtensions.cs`
- `specs/features/002-agent-framework-integration/implementation-evidence.md`

## Checkpoint: Feature Complete

- [x] Every Feature 002 acceptance criterion has recorded evidence.
- [x] Full tests and Release build pass on Microsoft Testing Platform.
- [x] Public API remains exactly the specified adapter, method, and two builder overloads.
- [ ] Human review approves Feature 002 before Feature 004 consumes it.

## Plan Approval

- [ ] Approve empty `Text` for hits without usable extracts.
- [ ] Approve build-time adapter resolution and one provider per built agent.
- [ ] Approve the real advertised function as the on-demand test boundary.
- [ ] Approve this plan and authorize Task 1 implementation.