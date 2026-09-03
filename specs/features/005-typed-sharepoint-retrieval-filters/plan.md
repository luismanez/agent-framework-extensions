# Implementation Plan: Feature 005 - Typed SharePoint Retrieval Filters

## Overview

Add the exact immutable `SharePointRetrievalFilter` public contract to the existing retrieval package. The type creates only canonical `Path`, `SiteID`, and `OR` expressions from trusted typed values, remains assignable to Feature 001's raw `FilterExpression`, and adds no parser, authorization semantics, dependency, project, DI registration, or HTTP behavior.

This plan is local to Feature 005. The executable checklist is [`todo.md`](todo.md), and the governing specification is [`005-typed-sharepoint-retrieval-filters.md`](005-typed-sharepoint-retrieval-filters.md).

## Planning Baseline

- Feature 001's `Microsoft365RetrievalOptions.FilterExpression` remains the only integration boundary and serializes the generated string unchanged.
- `Path(Uri)` uses the supplied URI's `AbsoluteUri`; it does not decode, rebuild, trim, or append path text.
- `Path` accepts only absolute HTTPS URIs with no user information, query, or fragment.
- `SiteId(Guid)` uses `siteId.ToString("D", CultureInfo.InvariantCulture).ToLowerInvariant()` or an equivalent canonical lowercase invariant operation.
- `AnyOf` stores and flattens internal typed terms. It never parses a previously generated `Expression` and never accepts raw KQL.
- All behavior is synchronous, deterministic, dependency-free, and testable without credentials or network access.

## Plan Gate Evidence

### `System.Uri` Boundary

The implementation treats `AbsoluteUri` as the canonical BCL output and validates components through structured properties. Focused tests record these representative outcomes before behavior is accepted:

| Input category | `Uri` observation used by the factory | Required factory result |
| --- | --- | --- |
| `https://contoso.sharepoint.com/sites/engineering/` | `AbsoluteUri` retains the trailing slash | Preserve it in `Path:".../"` |
| `https://contoso.sharepoint.com/sites/engineering` | `AbsoluteUri` has no trailing slash | Do not add one |
| Unicode path text | `AbsoluteUri` emits its escaped absolute representation | Use that escaped representation unchanged |
| Path containing `%22` | `AbsoluteUri` retains an escaped quote as URI data | Keep `%22`; never decode it into KQL syntax |
| URI with `?view=all` | `Query` is non-empty | Reject synchronously |
| URI with `#section` | `Fragment` is non-empty | Reject synchronously |

The test uses concrete expected strings produced by the pinned .NET 10 runtime. If the runtime observation differs from this table's semantics, implementation pauses rather than adding custom URI parsing.

### Canonical KQL

Official Retrieval API examples and supported-property documentation establish `Path`, `SiteID`, quoted values, and Boolean `OR`. This feature emits exactly:

```text
Path:"{AbsoluteUri}"
SiteID:"{lowercase-D-guid}"
(term1 OR term2 OR term3)
```

A single `AnyOf` input returns that filter's expression unchanged. Nested composed inputs are flattened from retained typed terms, so `AnyOf(A, AnyOf(B, C))` deterministically becomes `(A OR B OR C)` while preserving caller order and duplicates.

## Scope Boundaries

### In Scope

- One sealed public type with a private constructor, read-only `Expression`, and the three exact factory methods.
- BCL-only URI and GUID validation, canonical formatting, immutable internal term storage, and deterministic flattening.
- Public-surface, factory, composition, adversarial URI-data, Feature 001 integration, documentation, and evidence tests.

### Out of Scope

- Raw-expression construction, implicit conversion, public mutation, general KQL parsing, additional properties, or operators other than `OR`.
- URI tenant allowlists, authorization, permission evaluation, post-filtering, request execution, token acquisition, or option mutation.
- A local expression-length limit, new dependency, project, interface, options type, or DI registration.

## Architecture Decisions

### 1. Exact Public Surface

Expose only the specified sealed class, read-only `Expression`, and static `Path`, `SiteId`, and `AnyOf` factories. Construction remains private. Store an immutable snapshot of primitive term strings internally so composed filters can be flattened without parsing KQL and caller arrays cannot mutate created values.

Public API tests compile consumer-style calls and use reflection to reject public constructors, setters, raw-string factories, implicit conversions, extra public methods, and inheritance.

### 2. Path Validation and Formatting

Validate with `System.Uri` properties in this order:

1. reject null;
2. require `IsAbsoluteUri`;
3. require `Scheme == Uri.UriSchemeHttps` using ordinal case-insensitive comparison;
4. reject non-empty `UserInfo`, `Query`, or `Fragment`;
5. format `Path:"{path.AbsoluteUri}"`.

Do not validate the host as a tenant boundary. Do not call `UnescapeDataString`, inspect the original input string, manually split components, or alter trailing slashes. Percent-encoded quote and KQL-looking path segments remain escaped URI data.

### 3. Site-ID Validation and Formatting

Reject `Guid.Empty` synchronously. Format a non-empty identifier as lowercase invariant `D` and emit `SiteID:"{siteId}"`. No Graph lookup or site resolution occurs.

### 4. OR Composition

Reject a null array with `ArgumentNullException`; reject empty arrays and null elements with `ArgumentException`. Snapshot inputs during the call. A single filter returns a new immutable value with the same `Expression` and terms; multiple filters flatten all retained term lists and wrap one uppercase ` OR ` join in one pair of parentheses.

Preserve input order and duplicates. Do not simplify, sort, deduplicate, or infer equivalence. The internal term representation is an implementation detail and adds no public API.

### 5. Error and Security Semantics

Invalid inputs fail before a filter is created. Messages identify the invalid category but do not deliberately interpolate URI or tenant values. No filter-expression length limit is added because the Retrieval API publishes no corresponding `filterExpression` limit.

Documentation states that inputs must be trusted application values, filtering narrows retrieval but is not authorization, delegated Microsoft 365 permission trimming remains authoritative, and callers still own endpoint and business authorization. Raw `FilterExpression` remains available for advanced scenarios.

## Dependency Graph

```text
Feature 001 options contract
        |
        v
Task 1: exact public surface
        |
        v
Task 2: Path and SiteId factories
        |
        v
Task 3: immutable AnyOf composition
        |
        v
Task 4: Feature 001 integration and guidance
        |
        v
Task 5: evidence and closure
```

## Task Plan

| Task | Outcome | Size | Depends on |
| --- | --- | --- | --- |
| 1 | Lock the exact immutable public surface | S | Feature 001 |
| 2 | Implement canonical path and site-ID terms | M | Task 1 |
| 3 | Implement ordered, duplicate-preserving flattened OR composition | M | Task 2 |
| 4 | Prove Feature 001 compatibility and document security boundaries | M | Task 3 |
| 5 | Run full gates and record implementation evidence | S | Task 4 |

Detailed acceptance criteria, likely files, and commands are maintained in [`todo.md`](todo.md).

## Acceptance-Criteria Coverage

| Feature criterion | Planned evidence |
| --- | --- |
| Exact immutable public API | Task 1 consumer and reflection tests |
| Deterministic HTTPS path expression | Task 2 URI matrix tests |
| Canonical lowercase site ID | Task 2 GUID tests |
| Deterministic one-or-more `OR` composition | Task 3 composition tests |
| Synchronous documented failures | Tasks 2 and 3 validation tests |
| URI data cannot inject KQL terms | Task 2 adversarial escaped-data tests |
| Feature 001 assignment and serialization unchanged | Task 4 integration test |
| Filtering security and raw option documented | Task 4 documentation review |
| Credential-free focused and Release gates | Every checkpoint and Task 5 |

## Risks and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| URI decoding turns data into KQL syntax | High | Use `AbsoluteUri` unchanged and test `%22` plus KQL-looking segments |
| Composition reparses or corrupts nested expressions | High | Retain immutable primitive terms and flatten those terms only |
| URI normalization changes caller intent | Medium | Preserve the supplied `Uri.AbsoluteUri` and trailing-slash semantics |
| A filter is presented as authorization | High | Enforce naming and documentation language with review assertions |
| Public convenience APIs expand the raw-KQL surface | Medium | Reflection-test the exact specified public contract |
| Caller mutation changes an existing filter | Medium | Snapshot the params array and internal term data |

## Plan Approval

Human approval is required before Task 1 implementation. Approval accepts:

1. `Uri.AbsoluteUri` as the complete path serialization boundary;
2. rejection of non-HTTPS, user-info, query, and fragment URI components;
3. canonical lowercase invariant `D` GUID formatting;
4. deterministic flattening of nested `AnyOf` terms while preserving order and duplicates;
5. no expression length limit, new dependency, or public escape hatch.

## Task-Level Definition of Done

A task counts as complete only when its focused test is observed RED before implementation and GREEN afterward, its Release build succeeds, it touches no more than five files, public APIs have XML documentation, no dependency or live service is introduced, and its item in [`todo.md`](todo.md) is updated immediately.

## Authoritative Sources

- Feature specification: [`005-typed-sharepoint-retrieval-filters.md`](005-typed-sharepoint-retrieval-filters.md)
- Retrieval API filter properties: https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/copilotroot-retrieval
- Retrieval permission trimming: https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/overview
- SharePoint KQL syntax: https://learn.microsoft.com/en-us/sharepoint/dev/general-development/keyword-query-language-kql-syntax-reference
- .NET 10 `System.Uri` behavior verified by focused tests on the repository-pinned SDK