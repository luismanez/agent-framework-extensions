# Implementation Evidence: Feature 002 - Agent Framework Integration

**Worktree reference:** `uncommitted`
**Recorded:** 2026-09-04
**Status:** The chat-client integration supports both Agent Framework retrieval behaviors.

| Requirement | Test or command | Observed result | Commit/worktree reference |
| --- | --- | --- | --- |
| Adapter method and builder public surface compile | `AgentFrameworkPublicContractTests` | Passed | `uncommitted` |
| Results map one-to-one with source-name fallbacks, empty text, source links, and raw-hit identity | `Microsoft365RetrievalSearchTests` | Passed | `uncommitted` |
| Sensitivity metadata is not projected into model-visible text | `Microsoft365RetrievalSearchTests` | Passed | `uncommitted` |
| Builder resolves the adapter only during `Build` and rejects null configuration | `Microsoft365RetrievalChatClientBuilderExtensionsTests` | Passed | `uncommitted` |
| DI registers a transient search adapter | `Microsoft365RetrievalChatClientBuilderExtensionsTests` | Passed | `uncommitted` |
| Automatic retrieval runs before the inner agent and supplies framework-formatted context | `Microsoft365RetrievalAgentBehaviorTests` | Passed | `uncommitted` |
| Full Release test suite | `dotnet test --solution Acterion.Agents.AI.slnx --configuration Release --verbosity quiet` | Passed: 59 tests, 0 failed, 0 skipped | `uncommitted` |
| Release compilation | `dotnet build Acterion.Agents.AI.slnx --configuration Release` | Passed; 0 warnings, 0 errors | `uncommitted` |
| Change quality | `git diff --check` and five-axis review | Passed; no required findings | `uncommitted` |
| Both retrieval behaviors construct through the full context-provider pipeline | `Microsoft365RetrievalChatClientBuilderExtensionsTests` | Passed for `BeforeAIInvoke` and `OnDemandFunctionCalling` | `uncommitted` |
| On-demand retrieval advertises and invokes the framework search function | `Microsoft365RetrievalAgentBehaviorTests` | Passed: no eager retrieval, then retrieval and a second model call | `uncommitted` |
| On-demand retrieval preserves the model query, cancellation, and tool result | `Microsoft365RetrievalAgentBehaviorTests` | Passed: query and cancellation reach retrieval; result reaches the next model turn | `uncommitted` |
| Retrieval preserves surrounding chat-client pipeline stages | `Microsoft365RetrievalChatClientBuilderExtensionsTests` | Passed: stages before and after retrieval remain active | `uncommitted` |

## Boundary Decision

`UseMicrosoft365Retrieval` decorates `IChatClient` through `ChatClientBuilder.UseAIContextProviders`, which accepts the complete `AIContextProvider` contract. When on-demand retrieval is selected, it adds Agent Framework's native `UseFunctionInvocation` decorator after that provider so it receives the augmented tool options. The caller creates the `ChatClientAgent` with `UseProvidedChatClientAsIs = true` for this mode, preventing a second outer invoker from handling the call before the provider-owned tool is available. `TextSearchProvider` therefore advertises and invokes its own search function without a package-specific tool or parallel RAG lifecycle.

The adapter keeps retrieved text as untrusted `TextSearchProvider` data. It does not promote content to system instructions, create a custom prompt/tool/citation type, inspect ranking, or project sensitivity metadata.
