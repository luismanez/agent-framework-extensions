# Feature 002: Agent Framework Integration

**Parent specification:** [`SPEC.md`](../../SPEC/SPEC.md)  
**Status:** Draft  
**Depends on:** Feature 001  
**Enables:** Feature 004

## Objective

Adapt Microsoft 365 retrieval hits to Microsoft Agent Framework's existing `TextSearchProvider` contract so agents can use permission-trimmed SharePoint grounding automatically or on demand without introducing another RAG lifecycle.

## User Outcome

A developer can resolve `Microsoft365RetrievalSearch`, pass `SearchAsync` to `TextSearchProvider`, and select either `BeforeAIInvoke` or `OnDemandFunctionCalling` using standard Agent Framework options.

## Global Requirements Inherited

This feature MUST comply with the parent specification, especially:

- §7, package integration boundary;
- §9, response mapping;
- §10, Agent Framework integration and both retrieval behaviors;
- §11.3, search adapter responsibility;
- §13.3, indirect prompt injection;
- §19, .NET 10 and stable package baseline;
- §23.1, mapping tests;
- ADR-003 and §28, reuse and YAGNI constraints.

Feature 001 defines the retrieval-client and raw-hit contracts consumed here. If this document conflicts with the parent specification, the parent specification wins.

## Scope

### In Scope

- `Microsoft365RetrievalSearch` as the adapter from Feature 001 results to `TextSearchProvider.TextSearchResult`.
- One retrieval hit mapped to one search result.
- Source-name fallback, source links, extract concatenation, and raw representation.
- Empty-result behavior.
- DI registration of the adapter.
- Documentation and tests proving compatibility with both Agent Framework search modes.
- An optional thin provider/factory only if the Plan gate proves it materially reduces correct setup boilerplate.

### Out of Scope

- HTTP, token acquisition, and Graph error translation owned by Feature 001.
- Authentication implementation owned by Feature 003.
- Agent creation, model-provider configuration, or an HTTP endpoint owned by Feature 004.
- A custom search tool, RAG pipeline, memory system, context injection format, citation engine, or agent abstraction.
- Ranking, deduplication, relevance filtering, summarization, or rewriting of retrieved content.

## Functional Requirements

### Search Contract

`Microsoft365RetrievalSearch.SearchAsync` MUST be directly compatible with:

```csharp
Func<
    string,
    CancellationToken,
    Task<IEnumerable<TextSearchProvider.TextSearchResult>>>
```

It MUST pass the query and cancellation token to `IMicrosoft365RetrievalClient` without changing their meaning.

### Result Mapping

For every retrieval hit, the adapter MUST create exactly one `TextSearchResult` with:

- `SourceLink` set to `webUrl`;
- `SourceName` set to non-empty metadata `title`;
- `SourceName` falling back to the decoded filename or last meaningful URI segment;
- `SourceName` falling back to `webUrl` when no useful segment exists;
- `Text` containing non-empty extract text in response order, separated by a single newline;
- `RawRepresentation` containing the typed retrieval hit from Feature 001.

Extract-array order MUST be preserved within each hit. The Retrieval API does not guarantee ranking or ordering across hits, so the adapter MUST preserve the received hit sequence without claiming stable order, sorting it, or applying relevance filtering.

Whitespace-only extracts MUST be omitted. A hit with no non-empty extracts MUST have deterministic behavior finalized in the Plan gate: either map it with empty text if required by Agent Framework or omit it if empty text is not useful. The chosen behavior MUST be documented and tested.

Sensitivity-label metadata MUST NOT be added to `Text`, source names, prompts, or logs by default.

An empty retrieval-hit collection MUST return an empty result sequence and MUST NOT throw.

### Agent Framework Behavior

The adapter MUST work unchanged when attached to a `TextSearchProvider` configured with:

- `TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke`;
- `TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling`.

Agent Framework remains responsible for search-input construction, recent-message memory, context formatting and injection, tool advertisement, model invocation, and citation prompting.

The package MUST NOT copy or wrap Agent Framework's on-demand search-tool implementation.

### Prompt-Injection Boundary

Retrieved `Text` is untrusted model context. The adapter MUST preserve it as data returned through `TextSearchProvider`; it MUST NOT promote retrieved content into system instructions or treat document instructions as trusted application policy.

## Public API Constraint

The required public surface for this feature is `Microsoft365RetrievalSearch` and its search delegate-compatible method. A `Microsoft365RetrievalProvider` convenience type MAY be added only when all of the following are true:

- it remains a thin constructor/factory around `TextSearchProvider`;
- it exposes rather than hides `TextSearchProviderOptions`;
- it does not create or own an agent;
- it removes recurring setup mistakes demonstrated by tests or the sample.

Otherwise, do not add it in v0.1.

## Code Conventions

- Keep mapping logic deterministic and side-effect free apart from the client call.
- Use existing `Microsoft.Agents.AI` types directly.
- Do not create duplicate citation, tool, context-provider, or memory types.
- Keep the adapter small enough to understand without knowledge of HTTP or authentication internals.

## Testing Strategy

Unit tests MUST cover:

- `webUrl` to `SourceLink`;
- metadata title and each source-name fallback;
- multiple extracts, ordering, separator, and whitespace omission;
- the finalized empty-extract behavior;
- empty hit collections;
- raw hit identity or equivalent preservation in `RawRepresentation`;
- absence of sensitivity metadata from LLM-visible text;
- cancellation propagation to the retrieval client;
- both supported `SearchTime` values using an in-memory fake search delegate and fake model boundary.

The behavior tests MUST prove these observable outcomes without network access:

- `BeforeAIInvoke` invokes the fake search delegate before the fake model and supplies formatted retrieval context to the model invocation;
- `OnDemandFunctionCalling` does not search eagerly, advertises the Agent Framework search function to the fake model, and invoking that function reaches the fake search delegate.

If the stable public Agent Framework API cannot expose one of these observations directly, the Plan gate MUST define the nearest public contract test and document the residual integration gap. Merely constructing `TextSearchProvider` is not sufficient proof.

Tests MUST mock Feature 001's client and MUST NOT perform network or token acquisition.

## Commands

```powershell
dotnet build Acterion.Agents.AI.slnx --configuration Release
dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release
dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*Microsoft365RetrievalSearchTests"
```

## Boundaries

### Always

- Reuse `TextSearchProvider` and its current stable contracts.
- Preserve source names, links, raw hits, extract order, and cancellation.
- Treat retrieved text as untrusted context.

### Ask First

- Add a convenience provider/factory.
- Customize default Agent Framework prompts or formatting.
- Drop hits based on relevance or missing fields.

### Never

- Build a parallel RAG lifecycle or search tool.
- Place retrieved content into system instructions.
- Add sensitivity metadata to model-visible text by default.
- Couple mapping code to ASP.NET Core or `Microsoft.Identity.Web`.

## Acceptance Criteria

- [ ] A Feature 001 hit maps to one `TextSearchResult` using the documented fields and fallbacks.
- [ ] Multiple extracts and empty/missing values have deterministic tested behavior.
- [ ] Empty retrieval results return an empty sequence.
- [ ] `SearchAsync` is directly usable by `TextSearchProvider` in both supported modes.
- [ ] Raw representation is retained while sensitivity metadata stays out of LLM-visible text.
- [ ] Focused tests and the full Release build pass without network access.

## Plan Gate Exit Evidence

- Compile a probe against centrally pinned `Microsoft.Agents.AI` that exercises `TextSearchResult`, `RawRepresentation`, both `SearchTime` values, and the public provider lifecycle used by the behavior tests.
- Decide the empty-extract behavior from that probe and add an explicit contract test before implementation proceeds.
- Omit `Microsoft365RetrievalProvider` by default. Add it only if the sample reveals a repeated, testable setup failure that a thin factory prevents.
