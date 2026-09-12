# Microsoft 365 Work Context Provider

**Status:** Promoted
**Capability map:** [`capability-map.md`](../../specs/features/007-microsoft365-work-context/capability-map.md)
**First module specification:** [`SPEC-work-context-core.md`](../../specs/features/007-microsoft365-work-context/SPEC-work-context-core.md)

## Problem Statement

How might we give .NET developers using Microsoft Agent Framework fresh, minimal, delegated Microsoft 365 work context for every agent invocation, without making the extension responsible for identity, authorization, memory, or data caching?

## Recommended Direction

Create a focused community package named `Acterion.Agents.AI.Microsoft365.WorkContext`. Its primary experience is a single `Microsoft365WorkContextProvider` that follows the Agent Framework `AIContextProvider` pattern and enriches each invocation with a fresh work-context snapshot obtained from Microsoft Graph.

V1 includes four independently configurable facets: User Profile, Manager, Work Settings, and Calendar. User Profile is enabled by default; the other facets require explicit opt-in. When multiple facets are enabled, the package normally retrieves them through one Microsoft Graph JSON batch. Calendar remains in V1 because it makes the value of the extension tangible, but its model-visible data is deliberately limited.

The package also exposes a small direct client contract returning a read-only `WorkContextSnapshot`. Best-effort processing is the default: one failed batch operation does not discard successful facets or block the agent. A global fail-fast option supports hosts that require stricter behavior.

## Key Assumptions to Validate

- [ ] Agent Framework's current `AIContextProvider` contract supports fresh pre-invocation context without relying on unstable internals; validate against the installed package API and a focused integration test.
- [ ] Profile-only defaults provide useful personalization while keeping initial permissions and disclosure conservative; validate with a minimal Console sample.
- [ ] Calendar metadata improves meeting-related responses without requiring bodies, previews, attachments, links, or attendee email addresses; validate with representative prompt scenarios.
- [ ] Microsoft Graph JSON batching produces one normal outbound request while preserving independent facet outcomes; validate success, partial failure, throttling, and malformed subresponses.
- [ ] The required delegated permission set is acceptable for common enterprise hosts; document and test behavior when individual permissions or consent are missing.
- [ ] A public snapshot is useful outside Agent Framework without turning the package into a general Microsoft Graph SDK; constrain it to the four V1 facets.

## MVP Scope

- New package: `Acterion.Agents.AI.Microsoft365.WorkContext`.
- Native Agent Framework integration through `Microsoft365WorkContextProvider`.
- Public direct client returning an immutable `WorkContextSnapshot`.
- Fresh snapshot per invocation; stateless provider and no Graph-data cache.
- Delegated user identity only, supplied through a host-owned token-provider abstraction.
- User Profile facet enabled by default.
- Manager, Work Settings, and Calendar facets available through explicit opt-in.
- Calendar window and result limit configurable with conservative validated defaults.
- Calendar exposes subject, start/end, time zone, location, organizer name, and attendee names only.
- Microsoft Graph `$batch` used when multiple enabled facets require multiple operations.
- Best-effort partial-failure behavior by default and optional global fail-fast behavior.
- Bounded, deterministic model-context rendering.
- Safe diagnostics that report operation and facet outcomes without tokens or personal values.
- Tenant-independent unit and integration-style tests using fake HTTP and token providers.
- Representative Console sample; an ASP.NET Core/OBO sample may reuse the repository's established host pattern if it remains small enough for V1.

## Not Doing (and Why)

- **Business authorization or security-boundary decisions** - context enrichment cannot replace host and tool authorization.
- **Token acquisition, consent, claims processing, or token caching** - these remain host, MSAL, or Microsoft Identity Web responsibilities.
- **Graph-data caching or cross-invocation memory** - V1 promises a fresh snapshot and avoids distributed invalidation concerns.
- **Application-only authentication** - the context represents the current signed-in user.
- **Calendar bodies, previews, attachments, online-meeting URLs, or attendee email addresses** - unnecessary exposure for the intended awareness scenarios.
- **Arbitrary Graph field selection** - it would weaken least-data guarantees and expand the compatibility surface.
- **Third-party facet plug-ins** - no proven need yet; internal modularity is sufficient.
- **Prompt-driven or model-driven facet selection** - it adds unpredictability and another trust boundary.
- **Retries, durable workflows, or distributed coordination** - hosts and HTTP resilience policies own those operational concerns.
- **A generic `Acterion.Agents.AI.MicrosoftGraph` package** - the V1 capability is work context, not a Graph SDK.
- **Combining this package with Microsoft 365 Retrieval** - personal work context and document grounding have different contracts, permissions, and failure semantics.

## Open Questions

- What exact calendar window and maximum event count should be the defaults?
- Should declined, cancelled, all-day, private, or free events be omitted or represented minimally?
- How should disabled, unavailable, empty, and failed facets differ in the public snapshot?
- Should strict mode fail on every requested-facet failure, or only on non-permission and non-not-found failures?
- What deterministic text structure should the provider emit to minimize tokens and reduce instruction-like interpretation?
- Is one Console sample sufficient for V1, or is an ASP.NET Core/OBO sample necessary to prove the delegated host boundary?

## Promotion

This idea was accepted and promoted to Feature 007. The approved capability map is the authoritative index for module ids and dependency order. The approved `work-context-core` specification governs the core client and snapshot contract; dependent module specifications will govern Agent Framework integration and the two reference hosts.