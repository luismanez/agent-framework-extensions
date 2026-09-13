# Implementation Plan: Microsoft 365 Work Context Core

## Overview

Implement the host-independent `Acterion.Agents.AI.Microsoft365.WorkContext` package governed by [`SPEC-work-context-core.md`](../SPEC-work-context-core.md). Work proceeds risk-first and test-first: close the live Microsoft Graph uncertainties, lock the public contract, establish direct and batch transport, deliver each facet, add deterministic failure semantics, and close with hosting, privacy, documentation, and package evidence.

The executable checklist is [`todo.md`](todo.md). This plan covers only `work-context-core`. The Agent Framework provider and both samples require their own approved specifications and plans after the core module is complete.

## Execution Protocol

- Every agent task has a hard 20-minute timebox, one observable outcome, and a maximum of five touched files.
- Tasks run autonomously in batches of at most three. Progress is reported after every task; execution continues without another prompt.
- Checkpoints are verification gates, not timed implementation tasks. They run focused tests, the full solution test command, a Release build, and diff hygiene when applicable.
- At 20 minutes, the current task stops at the nearest green/buildable boundary. Remaining work becomes a numbered continuation task and is reported at the current checkpoint.
- User input is required only for the coordinated live-tenant session, an approved-spec contradiction, an `Ask First` boundary, or a verification failure that cannot be repaired within the current timebox.
- Fresh-context reviews occur at three risk boundaries: public API, Graph/privacy behavior, and final closure. The user is not expected to review code.
- Git commits are not created without one-time authorization. If authorized, the recommended granularity is one atomic commit per completed checkpoint.

Expected user actions are: approve this plan and commit mode, provide one coordinated disposable-tenant session, approve any forced specification amendment, and sign off the completed module. The user can interrupt after any progress update.

## Planning Baseline

- .NET SDK `10.0.300`, target `net10.0`, nullable references, implicit usings, and deterministic builds.
- xUnit v3 `4.0.0` on Microsoft Testing Platform.
- Centrally managed `Microsoft.Extensions.*` packages at `10.0.11`.
- The new source and test projects do not exist.
- Existing tests provide local patterns for consumer-contract tests, in-memory HTTP handlers, token stubs, fake time, and collecting loggers.
- Focused test commands use exact fully qualified class names because wildcard class filters are unreliable in this repository.
- Normal tests require no tenant, credentials, Microsoft 365 license, wall clock, or network.

## Current Source Findings

Checked against Microsoft Learn on 2026-09-12:

- `/me/manager` still lists delegated `User.Read.All` as least privileged and documents `404 Not Found` when no manager is assigned.
- `calendarView` lists delegated `Calendars.ReadBasic`, returns UTC event times when `Prefer: outlook.timezone` is absent, supports `$top`, and can return `@odata.nextLink`.
- `/me` lists delegated `User.Read`, and the mailbox-settings operation lists delegated `MailboxSettings.Read`, as least privileged.
- The current `calendarView` reference does not explicitly guarantee chronological default ordering or document `$orderby=start/dateTime`.
- JSON batching supports at most 20 requests, requires unique string ids, accepts relative URLs, can reorder subresponses, and requires each subresponse status to be evaluated independently of the outer status.

The permission baseline for all four facets is settled by the operation-specific authoritative permission tables. No further manual permission or tenant probes are required. Manager absence is also settled by its API reference; Calendar ordering and mailbox absence remain non-permission design decisions.

## Architecture Decisions

### Project Boundary

Create one packable source project and one non-packable test project. The package uses the BCL plus only the minimum centrally managed dependency-injection, HTTP, logging, and options packages proven necessary by compilation. It does not reference Microsoft Agent Framework, Microsoft Graph SDK, identity SDKs, ASP.NET Core, or a new abstractions package.

### Service Lifetime

Register the typed HTTP client and `IMicrosoft365WorkContextClient` as transient. Each resolved client retains only immutable configuration, `HttpClient`, `TimeProvider`, and stateless collaborators. The current scoped token provider is invoked per snapshot. Tokens, responses, mapped values, and snapshots remain method-local and cannot survive an invocation or cross scopes.

Preserve a host-registered `TimeProvider`; otherwise use `TimeProvider.System` without replacing host services.

The permitted client fields are `HttpClient`, `IMicrosoft365WorkContextTokenProvider`, an immutable options snapshot, `TimeProvider`, and `ILogger<Microsoft365WorkContextClient>`. Access-token strings, request or response messages, wire DTOs, operation outcomes, mapped facet values, and snapshots are forbidden as fields. A transient client can consume a scoped host token provider safely when resolved within that scope; singleton registration is not used. `TryAddSingleton(TimeProvider.System)` supplies the fallback while preserving a prior host registration.

### Request Pipeline

Create one fixed operation descriptor per stable id: `profile`, `manager`, `work-time-zone`, `work-language`, `work-hours`, and `calendar`. The descriptor owns only fixed facet ownership and package-owned URI construction. It is not a generic Graph client.

Zero operations return immediately. One operation uses direct `GET`. Two through six operations use one `POST v1.0/$batch` with structured JSON and no `dependsOn`. Direct and batch transport feed the same operation-outcome and facet-mapping pipeline.

### Response and Failure Pipeline

Parse direct or batch envelopes into internal operation outcomes. Correlate batch responses by id before mapping. Classify expected absence, HTTP failure, malformed payload, and success without retaining raw bodies. Facet reducers then apply best-effort or fail-fast semantics in fixed operation order.

Minimal wire DTOs declare only allowed properties. DTOs and raw JSON never cross the public boundary. Calendar processing separates valid-event mapping, eligibility/sorting, private-data redaction, attendee truncation, and malformed-entry policy so each concern has focused tests.

### Test Strategy

Every behavior task uses RED-GREEN-REFACTOR with one exact focused class filter. Tests assert public state and observable HTTP, not private method calls. Full solution tests and Release builds run at checkpoints, while task loops use only the cheapest focused test.

## Plan-Gate State Matrix

| Facet or boundary | Disabled | Successful data | Expected absence | Partial or malformed data in best effort | Real failure in best effort | Fail fast |
| --- | --- | --- | --- | --- | --- | --- |
| Profile | `Disabled` | `Available` value | N/A; no expected-absence status is defined | Malformed successful JSON is `Failed`, null value, `InvalidResponse` | `Failed`, null value | Throw on malformed data or real failure |
| Manager | `Disabled` | `Available` value | `Unavailable` for no manager | Malformed successful JSON is `Failed`, null value, `InvalidResponse` | `Failed`, null value | Absence returns; malformed data or real failure throws |
| Work Settings | `Disabled` | All-success or success-plus-absence is `Available` with successful values | `Unavailable` when every child is absent | Any malformed/failed child is `Failed`; preserve successful values, or null when none succeeded | Same partial/null rule; select failure by fixed child order | All-absence returns; first malformed/failed child by fixed order throws |
| Calendar | `Disabled` | `Available`, including an empty list | N/A; no expected-absence response is documented | Malformed event is `Failed` with all valid events, including an empty list | Facet/HTTP failure is `Failed`, null value | Malformed event or real failure throws |
| Global token, transport, outer HTTP, or outer batch | Disabled facets remain `Disabled` | N/A | N/A | N/A | Every enabled facet is `Failed`, null value | Throw one sanitized global exception; no facet ordering applies |
| Cancellation | No result is constructed | No result is constructed | No result is constructed | No result is constructed | Propagate `OperationCanceledException` unchanged | Same |
| Invalid local options | No result is constructed | No result is constructed | No result is constructed | No result is constructed | Throw before token acquisition or HTTP | Same |

For operation-level fail fast, select the first real failure in the fixed order `profile`, `manager`, `work-time-zone`, `work-language`, `work-hours`, `calendar`, independently of batch response order. Impossible cells are explicitly marked N/A rather than inferred.

## Threat Model

| Threat | Control | Evidence owner |
| --- | --- | --- |
| Delegated token disclosure | Header-only use, operation-local lifetime, no unsafe inner exceptions | Tasks 9, 13, 17-18, 24, 27, 29 |
| Excess personal-data retrieval | Fixed `$select` allowlists and minimal nested DTOs | Tasks 9-11, 19, 22 |
| Private meeting disclosure | Fail-closed sensitivity and explicit descriptive-field redaction | Tasks 22-23 |
| Attendee address retention | Name-only DTO projection, payload canaries, and no address public member | Tasks 7, 22, 27 |
| Cross-user state leakage | Transient client, scoped token provider, no token/snapshot fields | Tasks 29-30 |
| Batch response confusion | Fixed unique ids, id correlation, missing/duplicate rejection | Tasks 13-16 |
| Diagnostic data leakage | Allowlisted dimensions and sanitized failures | Tasks 17-18, 24, 26-27 |
| Prompt-like, control-character, or long Graph strings | Core maps strings only as inert data and never interprets or logs them; dependent provider owns rendering and bounds | Tasks 9, 21, 27, 31 and provider specification |

## Dependency Graph

```text
Plan Gate: G1-G9
       |
       v
Projects and contracts: Tasks 1-7
       |
       v
Options and direct facets: Tasks 8-12
       |
       v
Batch transport: Tasks 13-15
       |
       v
Outcome policy and rich facets: Tasks 16-23
       |
       v
Strict mode and cancellation: Tasks 24-25
       |
       v
Logging, DI, public gate: Tasks 26-30
       |
       v
Documentation and closure: Tasks 31-33
```

## Autonomous Batches

| Batch | Tasks | Outcome | Blocking user action |
| --- | --- | --- | --- |
| A | G1-G3 | Local plan-gate evidence | None |
| B | G4-G6 | Authoritative permissions, documented Manager absence, and recorded Calendar field evidence | None |
| C | G7-G9 | Calendar order/absence decisions and request fixtures | Spec decision only; no manual tenant testing |
| D | 1-3 | Projects and public primitives | None |
| E | 4-6 | Facet models | None |
| F | 7-9 | Snapshot, options, Profile | None |
| G | 10-12 | Manager and Calendar request | None |
| H | 13-15 | Operation selection and batch envelope | None |
| I | 16-18 | Classification and global/per-facet best effort | None |
| J | 19-21 | Work Settings and eligible Calendar mapping | None |
| K | 22-24 | Calendar privacy/malformed policy and fail fast | None |
| L | 25-27 | Cancellation and logging | None |
| M | 28-30 | DI, lifetime, and public API gate | None |
| N | 31-32 | Documentation and the closure evidence matrix | None |
| O | 33 | Record verified closure evidence after the release gate | Final sign-off |

Progress reports occur after each task and checkpoints after each batch. These are visibility updates, not approval prompts.

## Plan-Gate Evidence Record

Tasks G1-G9 append sanitized observations here before Task 1 begins:

| Evidence | Status | Observation |
| --- | --- | --- |
| Exact public API compiles against pinned graph | Passed, 2026-09-12 | Disposable `net10.0` Release build compiled every approved signature with nullable analysis, XML documentation, and warnings as errors; 0 warnings and 0 errors. |
| Approved dependency graph | Passed, 2026-09-12 | Direct packages are DI abstractions, HTTP, logging abstractions, and options at `10.0.11`; the resolved graph contains no Agent Framework, Graph SDK, identity SDK, ASP.NET Core, or new abstraction package. |
| Service lifetime analysis | Passed, 2026-09-12 | Transient client field allowlist excludes tokens, HTTP messages/responses, DTOs, outcomes, facet values, and snapshots. A compile probe verified typed `HttpClient` registration and non-replacing `TimeProvider.System` fallback. |
| Facet/mode state matrix | Passed, 2026-09-12 | Independent review found no missing facet/global state, failure-order rule, or response-order requirement. Live probes can still amend documented absence classifications. |
| Threat model and controls | Passed, 2026-09-12 | Independent review found no missing disclosure, privacy, cross-user, batch-confusion, diagnostic, or untrusted-string control owner. |
| Delegated permission baseline | Passed, 2026-09-13 | Operation-specific Microsoft Graph v1.0 permission tables identify `User.Read` for Profile, `User.Read.All` for Manager, `MailboxSettings.Read` for Work Settings, and `Calendars.ReadBasic` for Calendar. No manual permission comparison or broader permission is required. |
| Manager absence | Passed, 2026-09-13 | The authoritative List manager API reference specifies `404 Not Found` when no manager is assigned; the client classifies that documented absence as `Unavailable`. |
| Calendar field coverage | Passed, 2026-09-13 | A delegated `Calendars.ReadBasic` calendar-view request returned HTTP 200 and all 13 selected top-level event properties: subject, start, end, original start/end time zones, location, organizer, attendees, all-day/cancelled flags, sensitivity, availability, and response status. Nested contact and location details were observed but are intentionally excluded from evidence and will be discarded by minimal DTO projection. |
| Calendar ordering | Passed by approved decision, 2026-09-13 | The request uses documented `$top` without undocumented `$orderby`; the bounded page is sorted locally and is not described as the nearest events in the interval. |
| Calendar mailbox absence | Passed by approved decision, 2026-09-13 | The calendarView reference documents no mailbox-absence response. Calendar HTTP errors therefore remain sanitized failures; no Calendar response maps to `Unavailable` in V1. |
| Exact direct and six-operation batch requests | Passed, 2026-09-13 | An isolated .NET 10 probe generated identical output twice (SHA-256 `d02a4393eee8aea979ac003726fc1623ced5719eeeef5263ee909ba088762615`). The direct URI begins with `v1.0/`; all six batch URLs begin with `/me`; ids are stable and ordered; timestamps and query values are escaped; no `dependsOn` is present. |

### Canonical G9 Request Fixtures

Captured time: `2026-09-13T12:34:56.0000000+00:00`.

```text
direct=v1.0/me?%24select=displayName%2CgivenName%2Csurname%2CjobTitle%2Cdepartment%2CofficeLocation%2CpreferredLanguage
```

```json
{"requests":[{"id":"profile","method":"GET","url":"/me?%24select=displayName%2CgivenName%2Csurname%2CjobTitle%2Cdepartment%2CofficeLocation%2CpreferredLanguage"},{"id":"manager","method":"GET","url":"/me/manager?%24select=displayName%2CjobTitle%2Cdepartment%2CofficeLocation"},{"id":"work-time-zone","method":"GET","url":"/me/mailboxSettings/timeZone"},{"id":"work-language","method":"GET","url":"/me/mailboxSettings/language"},{"id":"work-hours","method":"GET","url":"/me/mailboxSettings/workingHours"},{"id":"calendar","method":"GET","url":"/me/calendar/calendarView?startDateTime=2026-09-13T12%3A34%3A56.0000000%2B00%3A00\u0026endDateTime=2026-09-14T12%3A34%3A56.0000000%2B00%3A00\u0026%24top=10\u0026%24select=subject%2Cstart%2Cend%2CoriginalStartTimeZone%2CoriginalEndTimeZone%2Clocation%2Corganizer%2Cattendees%2CisAllDay%2CisCancelled%2Csensitivity%2CshowAs%2CresponseStatus"}]}
```

## Checkpoint Gates

At each checkpoint:

1. run all focused tests added in the batch;
2. run `/Users/luisman/.dotnet/dotnet test --solution Acterion.Agents.AI.slnx --configuration Release`;
3. run `/Users/luisman/.dotnet/dotnet build Acterion.Agents.AI.slnx --configuration Release --no-incremental` when production or project files changed;
4. run `git diff --check`;
5. update [`todo.md`](todo.md) with observed counts and any timebox split.

Fresh-context review is additionally required after Task 7, Task 25, and Task 33.

## Risks and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Microsoft changes an operation-specific permission table | High | Recheck authoritative v1.0 references before release and amend the documented baseline if required |
| `$top` truncates before chronological ordering | High | Require a live-proven server query; never infer nearest events from local sorting |
| Direct and batch paths diverge | High | Shared operation outcomes and mapping; dual-path tests |
| Nested Graph fields leak personal data | High | Minimal DTOs, payload canaries, negative assertions |
| Error combinations become tangled | High | Separate classification, best effort, facet reduction, and fail fast tasks |
| A task exceeds 20 minutes | Medium | Stop green, create a continuation item, and preserve original acceptance criteria |
| Full builds consume micro-task time | Medium | Full gates are checkpoint work, outside task timeboxes |
| Too many approval prompts reduce autonomy | Medium | Only live/spec/Ask First/final gates block; progress reports continue automatically |

## Permission Evidence Policy

Operation-specific Microsoft Graph v1.0 permission tables are authoritative for least privilege. Profile uses delegated `User.Read`, Manager uses delegated `User.Read.All`, Work Settings uses delegated `MailboxSettings.Read`, and Calendar uses delegated `Calendars.ReadBasic`. No manual permission comparison or additional user-run tenant probe is required.

The previously recorded Calendar field observation contains no committed token, tenant identifier, user value, event value, or response body. G7 and G8 address non-permission Calendar semantics and must be resolved through conservative specification decisions rather than further manual testing.

## Approval Boundary

Authoritative evidence can force only these specification decisions:

1. adjust Manager permission or absence guidance;
2. reduce Calendar fields or approve `Calendars.Read`;
3. revise Calendar page or local-ordering semantics;
4. adjust documented Manager absence classification.

Any change requires a specification amendment and human approval. Everything else inside the approved plan proceeds autonomously.

## Plan Approval

- [x] Approved on 2026-09-12, authorizing G1-G9 and automatic continuation through Tasks 1-33 when the Plan Gate passes without a specification contradiction.
- [x] Plan Gate passed on 2026-09-13 after authoritative permission evidence, approved conservative Calendar decisions, and deterministic request fixtures.
- [x] Commit mode: no agent-created commits.

## Authoritative Sources

- Governing specification: [`SPEC-work-context-core.md`](../SPEC-work-context-core.md)
- Microsoft Graph manager: https://learn.microsoft.com/en-us/graph/api/user-list-manager?view=graph-rest-1.0
- Microsoft Graph calendar view: https://learn.microsoft.com/en-us/graph/api/calendar-list-calendarview?view=graph-rest-1.0
- Microsoft Graph event resource: https://learn.microsoft.com/en-us/graph/api/resources/event?view=graph-rest-1.0
- Microsoft Graph JSON batching: https://learn.microsoft.com/en-us/graph/json-batching
- Microsoft Graph throttling: https://learn.microsoft.com/en-us/graph/throttling
