# Task Checklist: Feature 008 - OneDrive Retrieval

**Specification:** [`008-onedrive-retrieval.md`](008-onedrive-retrieval.md)  
**Plan:** [`plan.md`](plan.md)  
**Status:** Implementation complete; awaiting human review

## Task 1: Configure and validate the source

**Acceptance:** A consumer can set `Microsoft365RetrievalDataSource.OneDriveBusiness`; the default remains `SharePoint`. DI and direct construction reject undefined values before token or HTTP calls. Construction snapshots the selected source.

**Verify:** Options and request tests pass, including a client whose source options are mutated after construction. These tests compile consumer-style use of the new enum from the separate test project.

**Likely files:** New enum in `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/`; `Microsoft365RetrievalOptions.cs`; `Microsoft365RetrievalOptionsValidator.cs`; `Microsoft365RetrievalOptionsTests.cs`; `Microsoft365RetrievalClientRequestTests.cs`.

- [x] Add enum and defaulted option with XML documentation.
- [x] Validate and snapshot only supported values.
- [x] Cover default, invalid-value, and public-consumer contracts.

## Task 2: Send OneDrive requests and reuse results

**Depends on:** Task 1.

**Acceptance:** A OneDrive client sends one v1.0 request with `dataSource: oneDriveBusiness`, the configured filter/metadata/result limit, and the delegated Bearer token. SharePoint JSON remains unchanged. OneDrive `driveItem` hits use existing models and map to Agent Framework results with a URL fallback when `title` is absent.

**Verify:** Run the focused command in `plan.md` after this task; inspect exact request JSON and confirm no `dataSourceConfiguration`, extra request, or new adapter API.

**Likely files:** `RetrievalWireModels.cs`; `Microsoft365RetrievalClient.cs`; `Microsoft365RetrievalClientRequestTests.cs`; `Microsoft365RetrievalClientResponseTests.cs`; `Microsoft365RetrievalSearchTests.cs`.

- [x] Map source enum to the exact Graph wire strings.
- [x] Test OneDrive request, response, and adapter fallback with existing fakes.
- [x] Confirm SharePoint regression tests and focused tests pass.

## Checkpoint: Core behavior

- [x] SharePoint remains the zero-configuration default.
- [x] Invalid source fails before token acquisition and HTTP.
- [x] OneDrive hit reaches the existing Agent Framework result shape.

## Task 3: Document the consumer contract

**Depends on:** Task 2.

**Acceptance:** Guides show source configuration and trusted OneDrive KQL, distinguish the OneDrive Copilot license requirement from SharePoint pay-as-you-go, explain delegated permissions and one source per request, and give source-specific troubleshooting steps.

**Verify:** Review examples against the new public API and the official links in the spec; `git diff --check` passes.

**Likely files:** `docs/configuration.md`; `docs/getting-started.md`; `docs/security.md`; `docs/troubleshooting.md`; root `README.md` if its package summary needs updating.

- [x] Update configuration and getting-started examples.
- [x] Update security and troubleshooting guidance.
- [x] Correct the root package summary if it still presents SharePoint as the only supported source.

## Task 4: Align samples and record evidence

**Depends on:** Tasks 2-3.

**Acceptance:** Sample guides state their actual configured source without suggesting a package-wide restriction. `implementation-evidence.md` records acceptance-criterion coverage, exact verification commands, and outcomes. No sample code changes unless the existing sample breaks.

**Verify:** Full solution tests, Release build, `git diff --check`, and final scope review pass.

**Likely files:** `samples/Microsoft365Retrieval.Console/README.md`; `samples/Microsoft365Retrieval.AspNetCore/README.md`; `implementation-evidence.md`.

- [x] Clarify sample-specific source and license wording where needed.
- [x] Run the equivalent full verification commands documented in `implementation-evidence.md`.
- [x] Record results and any unverified live-tenant behavior.

## Checkpoint: Feature complete

- [x] Every acceptance criterion in the spec has test, documentation, or recorded review evidence.
- [x] Full tests, Release build, and diff hygiene pass.
- [x] Changes remain limited to the existing Retrieval package, its tests, and its consumer guidance.
