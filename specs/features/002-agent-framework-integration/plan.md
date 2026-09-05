# Implementation Plan: Feature 002 - Agent Framework Integration

## Overview

Adapt Feature 001 retrieval hits to Microsoft Agent Framework `TextSearchProvider.TextSearchResult` values and expose the supported integration through `ChatClientBuilder.UseMicrosoft365Retrieval`. The implementation reuses Agent Framework's full context-provider lifecycle for automatic and on-demand retrieval; it does not introduce another RAG abstraction, tool, prompt, or agent type.

This plan is local to Feature 002. The executable checklist is [`todo.md`](todo.md), and the governing specification is [`002-agent-framework-integration.md`](002-agent-framework-integration.md).

## Planning Baseline

- Feature 001 must be implemented and approved before this plan starts.
- The centrally pinned `Microsoft.Agents.AI 1.19.0` package exposes `TextSearchProvider`, both `TextSearchBehavior` values, `RawRepresentation`, `ChatClientBuilder.Build(IServiceProvider)`, `ChatClientBuilder.UseAIContextProviders`, `UseFunctionInvocation`, and `IChatClient.AsAIAgent`.
- `TextSearchProvider` accepts a delegate compatible with `Microsoft365RetrievalSearch.SearchAsync`.
- Tests use xUnit v3 on Microsoft Testing Platform and must not require a model endpoint, Microsoft 365 tenant, token, or network connection.
- No production `.cs` implementation exists at planning time.

## Scope Boundaries

### In Scope

- `Microsoft365RetrievalSearch` and deterministic one-hit-to-one-result mapping.
- Both required `UseMicrosoft365Retrieval` overloads on `ChatClientBuilder`.
- Deferred service resolution and one `TextSearchProvider` instance per built chat-client pipeline.
- Public-contract, mapping, pipeline-composition, and both-mode behavior tests.
- Public XML documentation and feature-local implementation evidence.

### Out of Scope

- Retrieval HTTP behavior, token acquisition, model-provider setup, endpoint hosting, or typed KQL construction.
- Custom search tools, prompts, citation parsing, ranking, deduplication, memory, or result rewriting.
- A parameterless overload, package-specific Agent Framework options, `Microsoft365RetrievalProvider`, or a custom builder.

## Architecture Decisions

### 1. Adapter Lifetime and Contract

Register `Microsoft365RetrievalSearch` as a transient service. It depends only on `IMicrosoft365RetrievalClient`; `SearchAsync` forwards the query and cancellation token unchanged and returns materialized results in the received order.

The method signature remains directly assignable to:

```csharp
Func<string, CancellationToken, Task<IEnumerable<TextSearchProvider.TextSearchResult>>>
```

### 2. Result Mapping

- Map every hit to exactly one `TextSearchResult`, including a hit with no non-whitespace extracts.
- Set `Text` to non-whitespace extracts joined by one `\n`; when none remain, set it to `string.Empty`.
- Use a non-empty string `title` metadata value as `SourceName`.
- Otherwise parse `webUrl` as an absolute URI, select its last non-empty escaped path segment, then decode only that segment for display.
- Fall back to the original `webUrl` if parsing fails or no meaningful segment exists.
- Set `SourceLink` to the original `webUrl` and `RawRepresentation` to the same typed hit instance.
- Do not sort hits, inspect relevance, or copy sensitivity information into model-visible fields.

Keeping an empty-text result preserves the specification's one-hit-to-one-result invariant and avoids inventing relevance filtering. Agent Framework formatting remains authoritative.

### 3. Chat-Client Builder Composition

The behavior overload creates `TextSearchProviderOptions` with only `SearchTime` set and delegates to the options overload. The options overload rejects null arguments synchronously and adds a deferred middleware factory through `ChatClientBuilder.Use(Func<IChatClient, IServiceProvider, IChatClient>)`.

When `ChatClientBuilder.Build(IServiceProvider)` executes, the factory:

1. resolves `Microsoft365RetrievalSearch` with `GetRequiredService`;
2. creates one `TextSearchProvider` from `SearchAsync` and the caller-supplied options;
3. wraps the current inner chat client with a new `ChatClientBuilder(innerClient).UseAIContextProviders(provider)` pipeline;
4. appends native `UseFunctionInvocation` only when `SearchTime` is `OnDemandFunctionCalling`, so the invoker receives the provider-enriched tool options.

The caller then creates a `ChatClientAgent` through `AsAIAgent`. Automatic retrieval uses the normal agent options. On-demand retrieval MUST set `UseProvidedChatClientAsIs = true`, because the pipeline already contains its correctly ordered native function invoker. That setting disables all default agent decorators, so hosts must compose any additional required decorators on the chat-client builder. This makes missing registration fail at chat-client build time, preserves earlier and later pipeline stages, and prevents provider state from being shared across independently built pipelines.

### 4. Behavior-Test Boundary

Use a public-API fake `AIAgent` as the model boundary.

- For `BeforeAIInvoke`, record invocation order and the messages received by the fake agent; assert search runs first and formatted retrieval context reaches the invocation.
- For `OnDemandFunctionCalling`, record the advertised Agent Framework search function, assert no eager search, invoke it through the extension's native function-invocation pipeline, and assert that the result triggers the next model call.

These tests exercise the actual `TextSearchProvider` middleware. A construction-only assertion is insufficient. If a pinned package update removes public access to the advertised function, implementation pauses for human review rather than replacing framework behavior with a test-only custom tool.

## Dependency Graph

```text
Task 1: pin public contracts and framework probe
    |
    +--> Task 2: result mapping
             |
             +--> Task 3: builder composition
                      |
                      +--> Task 4: automatic retrieval behavior
                               |
                               +--> Task 5: on-demand and composition behavior
                                        |
                                        +--> Task 6: evidence and closure
```

## Task Plan

| Task | Outcome | Size | Depends on |
| --- | --- | --- | --- |
| 1 | Compile the public adapter and builder contracts against Agent Framework 1.19.0 | S | Feature 001 |
| 2 | Map Feature 001 hits deterministically | M | Task 1 |
| 3 | Add deferred DI-aware chat-client builder composition | M | Task 2 |
| 4 | Prove `BeforeAIInvoke` behavior through a fake model boundary | M | Task 3 |
| 5 | Prove on-demand behavior, pipeline preservation, and build-time diagnostics | M | Task 4 |
| 6 | Record evidence and close Feature 002 | S | Task 5 |

Detailed acceptance criteria, files, and commands are maintained in [`todo.md`](todo.md).

## Acceptance-Criteria Coverage

| Feature criterion | Planned evidence |
| --- | --- |
| One hit maps to one result with fallbacks | Task 2 mapping tests |
| Empty and multiple extracts are deterministic | Task 2 mapping tests |
| Empty hit collection succeeds | Task 2 mapping tests |
| Delegate works in both search modes | Tasks 4 and 5 behavior tests |
| Builder resolves services during `Build` and preserves composition | Tasks 3 and 5 builder tests |
| Raw hit retained and sensitivity data excluded | Task 2 mapping tests |
| No-network focused tests and Release build pass | Every checkpoint and Task 6 |

## Risks and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Agent Framework prerelease-era APIs change despite a stable pin | High | Compile public-contract and behavior probes before mapping implementation; require review for package changes |
| A shared context provider leaks state between built pipelines | High | Construct one provider inside the deferred build factory and test two builds |
| On-demand tests accidentally validate a custom tool | High | Invoke only the function advertised by the real `TextSearchProvider` |
| URI decoding changes path segmentation | Medium | Select the escaped segment first and decode only that display segment |
| Retrieved instructions gain application authority | High | Keep content only in `TextSearchResult.Text` and assert no system-message promotion |
| Empty extracts are silently dropped | Medium | Preserve one-to-one cardinality with explicit empty-text tests |

## Plan Approval

Human approval is required before Task 1 implementation. Approval accepts:

1. mapping hits with no usable extracts to `Text = string.Empty` rather than dropping them;
2. resolving `Microsoft365RetrievalSearch` only when `ChatClientBuilder.Build(IServiceProvider)` runs;
3. using the real framework-advertised function as the on-demand behavior-test boundary;
4. adding no parameterless overload or package-specific Agent Framework configuration type.

## Task-Level Definition of Done

A task counts as complete only when its focused test is observed RED before implementation and GREEN afterward, its Release build succeeds, it touches no more than five files, public APIs have XML documentation, no live service is required, and its item in [`todo.md`](todo.md) is updated immediately.

## Authoritative Sources

- Agent Framework RAG and `TextSearchProvider`: https://learn.microsoft.com/en-us/agent-framework/agents/rag
- `ChatClientBuilder` and `IChatClient` APIs: https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai.chatclientbuilder
- Restored package contract: `Microsoft.Agents.AI 1.19.0` XML documentation under the local NuGet cache
