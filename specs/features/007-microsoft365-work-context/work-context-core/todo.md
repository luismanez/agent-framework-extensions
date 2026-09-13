# Task Checklist: Microsoft 365 Work Context Core

**Specification:** [`SPEC-work-context-core.md`](../SPEC-work-context-core.md)
**Plan:** [`plan.md`](plan.md)
**Status:** Approved; Plan Gate awaiting live probes; no agent-created commits

A checked task has completed RED-GREEN verification and stayed within its hard 20-minute timebox. Checkpoints are untimed verification gates. Record observed results directly under the applicable item.

## Shared Commands

Focused test template:

```sh
/Users/luisman/.dotnet/dotnet test \
  --project tests/Acterion.Agents.AI.Microsoft365.WorkContext.Tests/Acterion.Agents.AI.Microsoft365.WorkContext.Tests.csproj \
  --configuration Release \
  --filter-class '<exact fully qualified class name>'
```

Checkpoint gate:

```sh
/Users/luisman/.dotnet/dotnet test --solution Acterion.Agents.AI.slnx --configuration Release
/Users/luisman/.dotnet/dotnet build Acterion.Agents.AI.slnx --configuration Release --no-incremental
git diff --check
```

## Human Preflight

- [ ] Prepare one disposable Entra app/session with the delegated consent variants required by the specification.
- [ ] Prepare synthetic users with and without a manager.
- [ ] Prepare one licensed mailbox with synthetic events and one account representing mailbox absence.
- [ ] Confirm that no live token, tenant/user identifier, event value, or response body will be recorded.

This prerequisite is user-owned setup, not a timeboxed agent task. G4-G8 run in one coordinated session.

## Phase 0: Plan-Gate Evidence

## Task G1: Compile Public Signatures

**Description:** Compile the exact approved public declarations in a disposable .NET 10 project outside tracked source.

**Acceptance criteria:**
- [x] All signatures, generic constraints, nullability, DI extension signature, and XML documentation compile.
- [x] No production implementation or tracked scratch artifact is created.

**Verification:**
- [x] Disposable Release build passed with 0 warnings and 0 errors; `git status --short` contained no scratch file.

**Dependencies:** Plan approval
**File cap:** 1 tracked file, [`plan.md`](plan.md) evidence only
**Timebox:** 15 minutes, hard stop at 20

## Task G2: Record Dependencies and Lifetime Analysis

**Description:** Resolve the minimum package graph and verify the proposed transient client against scoped multi-user hosts.

**Acceptance criteria:**
- [x] Dependency report excludes Agent Framework, Graph SDK, identity SDKs, ASP.NET Core, and new abstractions.
- [x] Constructor/field analysis proves tokens, responses, and snapshots remain operation-local.
- [x] Host `TimeProvider` preservation and fallback ownership are explicit.

**Verification:**
- [x] Sanitized dependency and lifetime observations are appended to the Plan-Gate Evidence Record.

**Dependencies:** G1
**File cap:** 1 tracked file, [`plan.md`](plan.md)
**Timebox:** 15 minutes, hard stop at 20

## Task G3: Finalize State and Threat Tables

**Description:** Review every facet/mode state and threat control against the approved specification before live probes.

**Acceptance criteria:**
- [x] Every disabled, successful, absent, partial, malformed, failed, cancelled, and invalid-options state is explicit or marked N/A.
- [x] Deterministic failure order and global-failure behavior are explicit.
- [x] Every identified disclosure or cross-user threat has a test owner.

**Verification:**
- [x] Independent plan review reported no missing Plan Gate item 3 or 4.

**Dependencies:** G2
**File cap:** 1 tracked file, [`plan.md`](plan.md)
**Timebox:** 15 minutes, hard stop at 20

## Checkpoint A: Local Plan Evidence

- [x] G1-G3 evidence is recorded.
- [x] Plan diagnostics and `git diff --check` pass.
- [ ] Human preflight is ready for one live session.

## Task G4: Probe Manager Permissions

**Description:** Compare the approved Manager request under delegated `User.Read` and `User.Read.All`.

**Acceptance criteria:**
- [ ] Record only scope set, HTTP status, required-field presence, and safe request id.
- [ ] A contradiction opens a spec amendment and stops dependent work.

**Verification:**
- [ ] Sanitized result is appended to the Plan-Gate Evidence Record.

**Dependencies:** Human preflight
**File cap:** 1 tracked file, [`plan.md`](plan.md)
**Timebox:** 20 minutes

## Task G5: Probe Manager Absence

**Description:** Observe `/me/manager` for the synthetic no-manager user.

**Acceptance criteria:**
- [ ] Status and classification are recorded without body or personal data.
- [ ] Result supports `Unavailable` or triggers a spec amendment.

**Verification:**
- [ ] Sanitized result is appended to the Plan-Gate Evidence Record.

**Dependencies:** Human preflight
**File cap:** 1 tracked file, [`plan.md`](plan.md)
**Timebox:** 10 minutes, hard stop at 20

## Task G6: Probe Calendar Basic Fields

**Description:** Request every approved Calendar field under delegated `Calendars.ReadBasic` using synthetic events.

**Acceptance criteria:**
- [ ] Every selected field is classified as returned, redacted, or unavailable.
- [ ] No event value or response body is recorded.
- [ ] Missing required fields trigger a spec amendment before Calendar code.

**Verification:**
- [ ] Sanitized field-coverage matrix is appended to the Plan-Gate Evidence Record.

**Dependencies:** Human preflight
**File cap:** 1 tracked file, [`plan.md`](plan.md)
**Timebox:** 20 minutes

## Checkpoint B: First Live Results

- [ ] G4-G6 completed in the same tenant session.
- [ ] Any contradiction has stopped implementation and been surfaced once.
- [ ] No sensitive value appears in the diff.

## Task G7: Probe Calendar Ordering

**Description:** Prove a server-supported chronological Calendar query before `$top` truncation.

**Acceptance criteria:**
- [ ] Synthetic events returned with a small `$top` prove server-side ascending start order.
- [ ] The exact accepted query shape is recorded, or the spec stops for amendment.

**Verification:**
- [ ] Sanitized ordering observation is appended to the Plan-Gate Evidence Record.

**Dependencies:** G6
**File cap:** 1 tracked file, [`plan.md`](plan.md)
**Timebox:** 20 minutes

## Task G8: Probe Mailbox Absence

**Description:** Observe Calendar behavior for the synthetic account without an applicable mailbox.

**Acceptance criteria:**
- [ ] Status and classification are recorded without response body or personal data.
- [ ] Result supports `Unavailable` or triggers a spec amendment.

**Verification:**
- [ ] Sanitized result is appended to the Plan-Gate Evidence Record.

**Dependencies:** Human preflight
**File cap:** 1 tracked file, [`plan.md`](plan.md)
**Timebox:** 10 minutes, hard stop at 20

## Task G9: Record Exact Request Fixtures

**Description:** After live decisions settle, generate canonical direct and six-operation batch requests with structured URI/JSON APIs in a disposable probe.

**Acceptance criteria:**
- [ ] Direct URI starts with `v1.0/`; batch URLs start with `/me`; ids and order match the spec.
- [ ] Calendar timestamps/query are invariant and escaped; batch has six requests and no `dependsOn`.
- [ ] Running twice with the same fake time produces identical output.

**Verification:**
- [ ] Redacted fixtures are recorded; no scratch source remains tracked.

**Dependencies:** G4-G8 and any required spec reapproval
**File cap:** 1 tracked file, [`plan.md`](plan.md)
**Timebox:** 20 minutes

## Checkpoint C: Plan Gate

- [ ] All seven Plan Gate exit-evidence requirements are satisfied.
- [ ] State and threat tables agree with live observations.
- [ ] Plan diagnostics, link checks, and `git diff --check` pass.
- [ ] No spec contradiction remains; plan approval automatically authorizes Tasks 1-33.

## Phase 1: Projects and Contracts

## Task 1: Scaffold Source Project

**Description:** Add the packable Work Context project to the solution with approved metadata and dependencies.

**Acceptance criteria:**
- [ ] Project targets `net10.0`, is packable, imports shared metadata, and generates XML docs.
- [ ] Solution includes the project and dependency graph matches G2.

**Verification:**
- [ ] Release source-project build passes.

**Dependencies:** Plan Gate
**File cap:** 3: solution, source project, central packages only if required
**Timebox:** 15 minutes, hard stop at 20

## Task 2: Scaffold Test Project

**Description:** Add the non-packable xUnit v3 test project and one baseline test to the solution.

**Acceptance criteria:**
- [ ] Test project references the source project and existing centrally pinned test dependencies.
- [ ] Exact focused filtering discovers and runs one baseline test.

**Verification:**
- [ ] Baseline test passes by fully qualified class name.

**Dependencies:** Task 1
**File cap:** 3: solution, test project, baseline test
**Timebox:** 10 minutes, hard stop at 20

## Task 3: Lock Enums, Options, and Interfaces

**Description:** Add RED consumer tests, then implement exact configuration and operation-boundary contracts.

**Acceptance criteria:**
- [ ] Enums, defaults, interfaces, namespace, nullability, and cancellation match the spec.
- [ ] No extra overload, setter beyond options, framework type, or host-specific API is public.
- [ ] Every public member has XML documentation.

**Verification:**
- [ ] RED then GREEN: `Acterion.Agents.AI.Microsoft365.WorkContext.Tests.PublicContract.WorkContextConfigurationContractTests`.

**Dependencies:** Task 2
**File cap:** 5: enum file, options, two interfaces, test
**Timebox:** 20 minutes

## Checkpoint D: Project Foundation

- [ ] Tasks 1-3 focused tests, full solution tests, Release build, and diff hygiene pass.

## Task 4: Define Result Primitives

**Description:** Implement facet result, facet failure, and package exception without creating the snapshot yet.

**Acceptance criteria:**
- [ ] Types are sealed, read-only, and not publicly constructible.
- [ ] Valid status/value/failure states are constructible internally; invalid states are rejected.
- [ ] Exception exposes only approved safe properties and no unsafe inner exception.

**Verification:**
- [ ] RED then GREEN: `Acterion.Agents.AI.Microsoft365.WorkContext.Tests.PublicContract.WorkContextResultPrimitiveContractTests`.

**Dependencies:** Task 3
**File cap:** 4: three source files and test
**Timebox:** 20 minutes

## Task 5: Define Profile and Manager Models

**Description:** Add immutable allowlisted Profile and Manager values.

**Acceptance criteria:**
- [ ] Exact nullable properties match the spec.
- [ ] No id, address, UPN, phone, or public constructor/setter exists.

**Verification:**
- [ ] RED then GREEN: Profile/Manager cases in `WorkContextModelContractTests`.

**Dependencies:** Task 4
**File cap:** 3: two models and test
**Timebox:** 10 minutes, hard stop at 20

## Task 6: Define Work Settings Models

**Description:** Add immutable Work Settings, locale, and working-hours values.

**Acceptance criteria:**
- [ ] Exact properties/nullability match the spec and time-zone strings remain opaque.
- [ ] Working days are copied into an immutable snapshot.

**Verification:**
- [ ] RED then GREEN: Work Settings cases in `WorkContextModelContractTests`.

**Dependencies:** Task 4
**File cap:** 4: three models and test
**Timebox:** 15 minutes, hard stop at 20

## Checkpoint E: Core Models I

- [ ] Tasks 4-6 focused tests, full solution tests, Release build, and diff hygiene pass.

## Task 7: Define Calendar and Snapshot Models

**Description:** Add the immutable Calendar event and complete snapshot after every referenced model exists.

**Acceptance criteria:**
- [ ] Calendar exposes only approved fields and copies attendee names.
- [ ] Snapshot exposes capture time and exactly four facet results.
- [ ] Complete model surface is sealed, read-only, and internally constructed.

**Verification:**
- [ ] RED then GREEN: Calendar/snapshot cases in `WorkContextModelContractTests`.
- [ ] Fresh-context public/privacy review has no blocking finding.

**Dependencies:** Tasks 5-6
**File cap:** 3: Calendar model, snapshot, test
**Timebox:** 15 minutes, hard stop at 20

## Task 8: Validate Options and Disabled Path

**Description:** Snapshot and validate options at client creation, capture fake UTC time, and return all-disabled results without external work.

**Acceptance criteria:**
- [ ] Bounds and enum validation precede token/HTTP even when Calendar is disabled.
- [ ] Later option mutation cannot alter the client.
- [ ] All-disabled returns four `Disabled` results with zero token/HTTP calls.

**Verification:**
- [ ] RED then GREEN: `Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext.Microsoft365WorkContextOptionsTests`.

**Dependencies:** Task 7
**File cap:** 5: client, validator/snapshot, test, fake token, fake time
**Timebox:** 20 minutes

## Task 9: Deliver Direct Profile

**Description:** Complete the default direct path from delegated token through exact Profile request and immutable mapping.

**Acceptance criteria:**
- [ ] One operation sends one `GET` with exact URI, allowlist, and Bearer token.
- [ ] Only seven approved fields map; forbidden/unknown fields do not escape, and prompt-like, control-character, or long strings remain inert data without changing control flow.
- [ ] Profile is `Available`; other facets are `Disabled`.

**Verification:**
- [ ] RED then GREEN: `Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext.Microsoft365WorkContextProfileTests`.

**Dependencies:** Task 8
**File cap:** 5: client, operation descriptor, Profile DTO, test, HTTP fake
**Timebox:** 20 minutes

## Checkpoint F: First Vertical Slice

- [ ] Tasks 7-9 focused tests, full solution tests, Release build, and diff hygiene pass.

## Task 10: Deliver Direct Manager

**Description:** Implement exact immediate-Manager request, mapping, and no-manager absence on the one-operation path.

**Acceptance criteria:**
- [ ] Four allowed fields map and forbidden identity/contact fields do not.
- [ ] Approved no-manager status becomes `Unavailable` in both modes.
- [ ] Direct request matches the G9 fixture.

**Verification:**
- [ ] RED then GREEN: `Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext.Microsoft365WorkContextManagerTests`.

**Dependencies:** Task 9 and G4-G5
**File cap:** 4: Manager DTO, mapper/client, test, fixture helper if needed
**Timebox:** 15 minutes, hard stop at 20

## Task 11: Build Calendar Request

**Description:** Generate the exact direct Calendar URI from one captured fake UTC instant using the live-proven query.

**Acceptance criteria:**
- [ ] Start/end, look-ahead, `$top`, ordering, and `$select` are invariant and escaped.
- [ ] No timezone preference or forbidden field is requested.
- [ ] Direct request matches G9.

**Verification:**
- [ ] RED then GREEN: `Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext.Microsoft365WorkContextCalendarRequestTests`.

**Dependencies:** Task 9 and G6-G7
**File cap:** 3: Calendar operation builder, descriptor/client, test
**Timebox:** 15 minutes, hard stop at 20

## Task 12: Select Fixed Operations

**Description:** Build the stable operation list for every facet combination without sending requests.

**Acceptance criteria:**
- [ ] Ids and order are exactly profile, manager, three Work Settings children, Calendar.
- [ ] Every enablement combination selects only required operations with no duplicates.
- [ ] Zero/one/many selection is explicit and deterministic.

**Verification:**
- [ ] RED then GREEN: `Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext.WorkContextOperationSelectionTests`.

**Dependencies:** Tasks 10-11
**File cap:** 3: operation descriptor/list, tests
**Timebox:** 15 minutes, hard stop at 20

## Checkpoint G: Direct Operations

- [ ] Tasks 10-12 focused tests, full solution tests, Release build, and diff hygiene pass.

## Task 13: Serialize Batch Request

**Description:** Serialize two through six selected operations into one structured JSON batch request.

**Acceptance criteria:**
- [ ] One token and one POST are used for every multi-operation combination.
- [ ] Stable unique ids, `/me` URLs, no `dependsOn`, and JSON content type match the spec.
- [ ] Six-operation payload matches G9.

**Verification:**
- [ ] RED then GREEN: `Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext.Microsoft365WorkContextBatchRequestTests`.

**Dependencies:** Task 12 and G9
**File cap:** 4: client, batch request DTO, serializer, test
**Timebox:** 20 minutes

## Task 14: Parse Batch Envelope

**Description:** Parse the outer batch response into minimal internal subresponse records without mapping facets.

**Acceptance criteria:**
- [ ] Outer success does not imply subresponse success.
- [ ] Malformed outer JSON/envelope is distinguished from subresponse bodies.
- [ ] Unknown body properties are ignored and raw bodies remain internal.

**Verification:**
- [ ] RED then GREEN: envelope cases in `Microsoft365WorkContextBatchResponseTests`.

**Dependencies:** Task 13
**File cap:** 3: batch response DTO/parser, test
**Timebox:** 15 minutes, hard stop at 20

## Task 15: Correlate Batch Responses

**Description:** Correlate parsed subresponses to requested operations independently of response order.

**Acceptance criteria:**
- [ ] Reordered responses correlate correctly.
- [ ] Missing/duplicate expected ids become invalid outcomes; unknown ids are ignored.
- [ ] Profile and Manager successful bodies reach their existing mappers through batch.

**Verification:**
- [ ] RED then GREEN: correlation cases in `Microsoft365WorkContextBatchResponseTests`.

**Dependencies:** Task 14
**File cap:** 4: correlator, client, batch response test, test fixture
**Timebox:** 20 minutes

## Checkpoint H: Batch Transport

- [ ] Tasks 13-15 focused tests, full solution tests, Release build, and diff hygiene pass.

## Task 16: Classify Operation Outcomes

**Description:** Normalize success, expected absence, HTTP status, malformed body, and safe request id at the operation boundary.

**Acceptance criteria:**
- [ ] `401`, `403`, `429`, `5xx`, other unexpected status, and approved `404` cases classify exactly.
- [ ] Request-id preference is deterministic and raw Graph errors are not retained.
- [ ] Classification itself does not choose best effort or fail fast.

**Verification:**
- [ ] RED then GREEN: `Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext.WorkContextOperationOutcomeTests`.

**Dependencies:** Task 15
**File cap:** 4: outcome type, classifier, test, response helper
**Timebox:** 20 minutes

## Task 17: Apply Best-Effort Global Failures

**Description:** Convert token, empty-token, transport, outer HTTP, and malformed outer batch failures into enabled-facet results.

**Acceptance criteria:**
- [ ] Every enabled facet becomes `Failed`; disabled facets remain `Disabled`.
- [ ] Failure kind/status/request id are safe; no source exception or body is retained.
- [ ] No automatic retry occurs.

**Verification:**
- [ ] RED then GREEN: global cases in `Microsoft365WorkContextBestEffortFailureTests`.

**Dependencies:** Task 16
**File cap:** 5: client, failure reducer, test, HTTP fake, token fake
**Timebox:** 20 minutes

## Task 18: Apply Best-Effort Facet Failures

**Description:** Reduce ordinary direct and batch operation failures to only their owning facet while preserving successful siblings across all four facets.

**Acceptance criteria:**
- [ ] A failed Profile, Manager, Work Settings child, or Calendar operation produces one owning `Failed` facet with safe failure metadata.
- [ ] Successful and unavailable sibling facets retain their values/statuses in batch mode.
- [ ] Direct and batch paths produce equivalent facet outcomes for the same failure.

**Verification:**
- [ ] RED then GREEN: facet cases in `Microsoft365WorkContextBestEffortFailureTests`.

**Dependencies:** Tasks 16-17
**File cap:** 3: facet reducer, test, fixture helper
**Timebox:** 15 minutes, hard stop at 20

## Checkpoint I: Best-Effort Foundation

- [ ] Tasks 16-18 focused tests, full solution tests, Release build, and diff hygiene pass.

## Task 19: Map Successful Work Settings

**Description:** Map the three narrow mailbox-setting responses and reduce all-success/all-absent outcomes.

**Acceptance criteria:**
- [ ] Only time zone, language, and working-hours fields/endpoints are used.
- [ ] Successful children combine immutably with opaque time-zone strings.
- [ ] All approved absent children produce `Unavailable`.

**Verification:**
- [ ] RED then GREEN: success/absence cases in `Microsoft365WorkContextWorkSettingsTests`.

**Dependencies:** Tasks 15-16
**File cap:** 5: settings DTOs, mapper/reducer, test
**Timebox:** 20 minutes

## Task 20: Reduce Partial Work Settings

**Description:** Apply deterministic best-effort behavior when one or more Work Settings child operations fail.

**Acceptance criteria:**
- [ ] Any failed child makes the facet `Failed` and selects failure by fixed child order.
- [ ] Successfully mapped siblings remain in a partial value; no success yields null value.
- [ ] Batch response order cannot change the result.

**Verification:**
- [ ] RED then GREEN: partial/failure cases in `Microsoft365WorkContextWorkSettingsTests`.

**Dependencies:** Tasks 18-19
**File cap:** 3: settings reducer, test, fixture helper
**Timebox:** 15 minutes, hard stop at 20

## Task 21: Map Eligible Calendar Events

**Description:** Map valid public events and apply cancellation, declined-response, all-day/free, sorting, and maximum rules.

**Acceptance criteria:**
- [ ] Cancelled/declined events are omitted; all-day/free events remain.
- [ ] Valid output sorts by start then end and never exceeds the configured maximum; prompt-like, control-character, or long strings remain inert data.
- [ ] Empty eligible output is `Available` with an immutable empty list.

**Verification:**
- [ ] RED then GREEN: eligibility cases in `Microsoft365WorkContextCalendarMappingTests`.

**Dependencies:** Tasks 11 and 16
**File cap:** 4: Calendar DTO, mapper, test, fixture helper
**Timebox:** 20 minutes

## Checkpoint J: Settings and Calendar Mapping

- [ ] Tasks 19-21 focused tests, full solution tests, Release build, and diff hygiene pass.

## Task 22: Enforce Calendar Privacy

**Description:** Redact private-event descriptions and map bounded public-event names without retaining nested contact data.

**Acceptance criteria:**
- [ ] Private subject/location/organizer/attendees are absent and truncation is false.
- [ ] Public attendee names preserve order, omit blanks, cap at 10, and set truncation correctly.
- [ ] Address/location/id/body/link canaries never reach public results.

**Verification:**
- [ ] RED then GREEN: privacy cases in `Microsoft365WorkContextCalendarPrivacyTests`.

**Dependencies:** Task 21
**File cap:** 4: Calendar DTO, mapper, privacy test, fixture helper
**Timebox:** 20 minutes

## Task 23: Handle Malformed Calendar Entries

**Description:** Apply fail-closed sensitivity and best-effort partial-list semantics to malformed individual events.

**Acceptance criteria:**
- [ ] Invalid/missing sensitivity or malformed required event structure omits that event.
- [ ] Facet is `Failed` with all valid events, including an empty list when none remain.
- [ ] HTTP/facet-level Calendar failure keeps null value.

**Verification:**
- [ ] RED then GREEN: malformed cases in `Microsoft365WorkContextCalendarPrivacyTests`.

**Dependencies:** Tasks 18 and 22
**File cap:** 3: Calendar mapper/reducer, test
**Timebox:** 15 minutes, hard stop at 20

## Task 24: Apply Fail Fast

**Description:** Convert the first real failure in fixed operation order into one sanitized package exception.

**Acceptance criteria:**
- [ ] Selection ignores batch response order and covers global and facet failures.
- [ ] Expected absence returns normally; malformed Calendar entries and real child failures throw.
- [ ] Exception exposes only approved safe fields and no unsafe inner exception.

**Verification:**
- [ ] RED then GREEN: `Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext.Microsoft365WorkContextFailFastTests`.

**Dependencies:** Tasks 20 and 23
**File cap:** 4: client, fail-fast reducer, test, fixture helper
**Timebox:** 20 minutes

## Checkpoint K: Calendar Privacy and Strict Mode

- [ ] Tasks 22-24 focused tests and all facet tests pass.
- [ ] Full solution tests, Release build, and diff hygiene pass.

## Task 25: Preserve Cancellation

**Description:** Prove cancellation passes unchanged through token acquisition, send, direct response read, and batch response read.

**Acceptance criteria:**
- [ ] Every cancellation point throws `OperationCanceledException` without wrapping or facet conversion.
- [ ] No completion/failure behavior or retry follows cancellation.

**Verification:**
- [ ] RED then GREEN: `Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext.Microsoft365WorkContextCancellationTests`.
- [ ] Fresh-context Graph/privacy/error review has no blocking finding.

**Dependencies:** Task 24
**File cap:** 4: client if required, cancellation test, HTTP fake, token fake
**Timebox:** 15 minutes, hard stop at 20

## Task 26: Emit Structured Logs

**Description:** Add stable start, completion, and failure events with only approved operational dimensions.

**Acceptance criteria:**
- [ ] Events report operation count, batch use, duration, facet statuses, safe status/request id, and event counts where applicable.
- [ ] Cancellation emits no misleading completion or failure event.
- [ ] Logging does not alter HTTP, snapshot, or exception behavior.

**Verification:**
- [ ] RED then GREEN: behavior cases in `Microsoft365WorkContextLoggingTests`.

**Dependencies:** Task 25
**File cap:** 4: client, log definitions, test, collecting logger
**Timebox:** 20 minutes

## Task 27: Prove Diagnostic Privacy and Resilience

**Description:** Test sensitive canaries across logs/exceptions and prove a throwing logger cannot change behavior.

**Acceptance criteria:**
- [ ] No token, scope, personal value, raw URL/JSON, Graph message, address, or private description appears.
- [ ] Throwing logger leaves success, failure, and cancellation outcomes unchanged.

**Verification:**
- [ ] RED then GREEN: privacy/resilience cases in `Microsoft365WorkContextLoggingTests`.

**Dependencies:** Task 26
**File cap:** 4: logging test, throwing logger, payload fixture, client only if required
**Timebox:** 15 minutes, hard stop at 20

## Checkpoint L: Cancellation and Logging

- [ ] Tasks 25-27 focused tests, full solution tests, Release build, and diff hygiene pass.
- [ ] State matrix has executable coverage and independent review approval.

## Task 28: Register Services

**Description:** Add DI registration for options, typed HTTP client, transient client, required token provider, and TimeProvider fallback.

**Acceptance criteria:**
- [ ] Null arguments fail synchronously and missing token provider fails clearly.
- [ ] Graph base address, options validation, transient client, host time provider, and fallback are correct.
- [ ] Registration does not replace host services or alter authentication/authorization.

**Verification:**
- [ ] RED then GREEN: registration cases in `Microsoft365WorkContextDependencyInjectionTests`.

**Dependencies:** Tasks 8 and 27
**File cap:** 4: service extension, source project, test, registration helper if needed
**Timebox:** 20 minutes

## Task 29: Prove Multi-User Lifetime Isolation

**Description:** Resolve two scopes with distinct token providers and prove no token, response, or snapshot crosses scope/invocation boundaries.

**Acceptance criteria:**
- [ ] Each scope sends only its own token and receives only its own synthetic payload.
- [ ] Repeated calls are fresh and client fields contain no token/snapshot/response state.
- [ ] Service-lifetime evidence agrees with G2.

**Verification:**
- [ ] RED then GREEN: lifetime cases in `Microsoft365WorkContextDependencyInjectionTests`.

**Dependencies:** Task 28
**File cap:** 3: DI test, scoped token fake, client only if required
**Timebox:** 15 minutes, hard stop at 20

## Task 30: Run Complete Public Consumer Gate

**Description:** Compile consumer-style usage of every public member after DI and all models exist, then reflection-test prohibited surface.

**Acceptance criteria:**
- [ ] Registration, options, interfaces, snapshots, every model, failure, and exception compile as specified.
- [ ] Defaults, sealed/read-only shape, internal construction, and XML docs are complete.
- [ ] No Graph/identity/framework/wire/raw JSON type leaks publicly.

**Verification:**
- [ ] RED then GREEN: `Acterion.Agents.AI.Microsoft365.WorkContext.Tests.PublicContract.WorkContextPublicContractTests`.

**Dependencies:** Tasks 7 and 28
**File cap:** 2: public-contract test and source member only if a defect is found
**Timebox:** 15 minutes, hard stop at 20

## Checkpoint M: DI and Public Contract

- [ ] Tasks 28-30 focused tests, full solution tests, Release build, dependency report, and diff hygiene pass.

## Task 31: Publish Core Package Guidance

**Description:** Document direct-client setup, delegated permissions, known Graph inconsistencies, data minimization, fresh/no-cache semantics, and host security ownership.

**Acceptance criteria:**
- [ ] Guidance covers Profile, Manager, Work Settings, Calendar, best effort/fail fast, DI, and token-provider ownership.
- [ ] Permissions and live findings are stated without overstating least privilege.
- [ ] Work context is explicitly untrusted enrichment, never authorization; no secret or tenant value is present.

**Verification:**
- [ ] Documentation checklist, link check, secret-pattern scan, and `git diff --check` pass.

**Dependencies:** Tasks 29-30
**File cap:** 3: package README, root package table/links, configuration or security doc
**Timebox:** 20 minutes

## Task 32: Prepare Closure Evidence Matrix

**Description:** Create the evidence document with one pending row for every Plan Gate item and success criterion before running the final commands.

**Acceptance criteria:**
- [ ] Every requirement maps to an exact focused test or final command.
- [ ] Rows include placeholders for observed result and worktree/commit reference without claiming unrun evidence.
- [ ] No requirement is duplicated or omitted.

**Verification:**
- [ ] Evidence coverage review finds no unmapped Plan Gate or success criterion.

**Dependencies:** Task 31
**File cap:** 2: `implementation-evidence.md`, this checklist
**Timebox:** 15 minutes, hard stop at 20

## Checkpoint N: Release and Package Gate

- [ ] Full Work Context tests and full solution tests pass.
- [ ] Release build and Work Context pack succeed.
- [ ] Package contents and dependency graph match the specification.
- [ ] Editor diagnostics, secret scan, links, and `git diff --check` are clean.
- [ ] Complete API and service-lifetime gates pass.

## Task 33: Record Closure Evidence

**Description:** Map every Plan Gate and specification success criterion to the observed Checkpoint N release results and complete an independent final review.

**Acceptance criteria:**
- [ ] Evidence records every requirement with command/test, observed result, and worktree/commit reference.
- [ ] Evidence contains no token, tenant/user/event value, raw response, or secret.
- [ ] Fresh-context review reports no unresolved critical or important finding.

**Verification:**
- [ ] Evidence coverage check, editor diagnostics, link check, and `git diff --check` pass.

**Dependencies:** Task 32 and completed Checkpoint N
**File cap:** 2: `implementation-evidence.md`, this checklist
**Timebox:** 20 minutes

## Checkpoint O: Module Complete

- [ ] Task 33 evidence and review pass.
- [ ] Every Plan Gate item and success criterion has evidence.
- [ ] Normal verification is tenant-, credential-, license-, wall-clock-, and network-independent.
- [ ] Public API exactly matches the approved specification.
- [ ] No prohibited dependency, field, cache, retry, paging, identity behavior, or Agent Framework integration exists.
- [ ] Human approves `work-context-core` before `agent-framework-provider` implementation begins.

## Plan Approval

- [x] Human approves [`plan.md`](plan.md), authorizing G1-G9 and automatic continuation through Tasks 1-33 when the Plan Gate passes without a spec contradiction.
- [x] Human chooses no agent-created commits.
