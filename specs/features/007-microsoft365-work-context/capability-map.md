# Capability Map: Microsoft 365 Work Context

**Origin:** [`microsoft365-work-context-provider.md`](../../../docs/ideas/microsoft365-work-context-provider.md)
**Status:** Approved for specification
**Target package:** `Acterion.Agents.AI.Microsoft365.WorkContext`

| Module id | Responsibility | Depends on |
| --- | --- | --- |
| `work-context-core` | Public options and token boundary, Microsoft Graph JSON batching, the four V1 facets, immutable snapshots, and best-effort or fail-fast error semantics | - |
| `agent-framework-provider` | Native `AIContextProvider` integration, deterministic model-context rendering, dependency injection, and chat-client builder integration | `work-context-core` |
| `console-sample` | Delegated device-code reference host for the complete provider | `work-context-core`, `agent-framework-provider` |
| `aspnetcore-obo-sample` | Protected ASP.NET Core API and delegated On-Behalf-Of reference host for the complete provider | `work-context-core`, `agent-framework-provider` |

Build order:

```text
work-context-core
        |
        v
agent-framework-provider
        |
        +----------------------+
        |                      |
        v                      v
console-sample       aspnetcore-obo-sample
```

The four facets are not separate modules. User Profile, Manager, Work Settings, and Calendar share one snapshot contract, one Graph batch lifecycle, one options model, and one partial-failure policy. Splitting them would duplicate boundaries and weaken the primary one-provider developer experience.

The two samples are separate modules because they have different consumers, identity dependencies, hosting lifecycles, tests, and acceptance criteria. They can be specified and implemented independently after the provider contract is approved.

## Initiative Constraints

- The package uses delegated Microsoft Graph identity only.
- The host owns authentication, consent, token acquisition, and token caching.
- The provider is stateless and retrieves a fresh snapshot for every invocation.
- V1 stores no Microsoft Graph data and creates no cross-invocation memory.
- Context enrichment is not authorization and is never a security boundary.
- Only an explicit, bounded subset of retrieved data is exposed to the model.
- Normal automated tests require no tenant, credentials, Microsoft 365 license, or network access.

## Specification Order

1. `SPEC-work-context-core.md`
2. `SPEC-agent-framework-provider.md`
3. `SPEC-console-sample.md` and `SPEC-aspnetcore-obo-sample.md`, which may proceed in parallel

Each module must complete its Specify, Plan, Tasks, and Implement gates before dependent implementation begins. The capability map is the authoritative index for module ids and dependency direction.