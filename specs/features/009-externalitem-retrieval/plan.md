# Implementation Plan: Feature 009 - ExternalItem Retrieval

## Overview

Add `ExternalItem` to the configured source of the existing Retrieval client, with optional connection-ID scoping, then expose it through the existing Console sample. Preserve SharePoint and OneDrive requests, the delegated-token boundary, and the Agent Framework result contract. The approved scope is in [`009-externalitem-retrieval.md`](009-externalitem-retrieval.md); execution is tracked in [`todo.md`](todo.md).

The feature fits **one implementation prompt** covering the four ordered tasks below. Run the focused package tests after Task 2 and all relevant tests plus the Release build at the end. No separate approval is needed between tasks unless implementation discovers a conflict with the approved spec.

## Architecture decisions

- Extend `Microsoft365RetrievalDataSource` with `ExternalItem = 2`. Add nullable `ExternalItemConnectionIds` to options; `null` means omit `dataSourceConfiguration`, while a nonempty collection produces Graph's nested `externalItem.connections` shape.
- Validate source compatibility, nonempty unique IDs, and absence of blank entries before token or HTTP work. Copy the IDs during the existing options snapshot. Preserve exact ID text and order.
- Keep the current endpoint, `RetrieveAsync`, token provider, hit/extract models, and Agent Framework adapter. Add connector response fixtures; change mapping only if they show a real gap. Correct source-specific XML wording where touched.
- Keep `ResourceMetadata` defaults for existing consumers. Connector examples choose fields common to all selected schemas, or `[]` with no KQL for heterogeneous or unknown schemas; for `ExternalItem`, `[]` omits Graph's optional `resourceMetadata` request property. The package does not inspect schemas or change fields per hit.
- Extend the current Console configuration and `--retrieval-only` path, including configurable metadata names and a URL fallback when no `title` is returned. The ASP.NET Core host already binds package options; its guide needs an example, not a new host or auth flow.

## Dependency order and tasks

| Task | Deliverable | Depends on | Expected scope |
| --- | --- | --- | --- |
| 1 | Public option, validation, snapshots, and contract tests | Approved spec | About 5 files |
| 2 | Exact Graph request and connector result/adapter tests | Task 1 | About 5 files |
| 3 | Existing Console configuration, tests, and runnable guidance | Task 2 | 4 files |
| 4 | Package and ASP.NET Core consumer guidance | Tasks 2-3 | 5 files |

### Checkpoint after Task 2

Run the Retrieval package test project. Confirm default SharePoint and existing OneDrive JSON remain identical, `ExternalItem` omits configuration when IDs are absent, configured IDs produce the documented nested JSON, empty metadata omits `resourceMetadata`, and invalid options fail before I/O. Confirm connector hits with absent `relevanceScore` reach the adapter without a new public model.

### Final checkpoint

Run the Console tests, full solution tests, Release build, and `git diff --check`. Review the changed public API and exact request JSON against the spec. Record commands, outcomes, and any unavailable live-tenant check in `implementation-evidence.md`. This checkpoint is part of the same implementation prompt, not another approval gate.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Empty IDs silently broaden a query | Reject a configured empty list; serialize no configuration only for `null` |
| Mutable options alter later requests | Snapshot the IDs and test mutation after client construction |
| SharePoint or OneDrive JSON regresses | Assert exact existing request bodies alongside the new connector cases |
| Connector schemas differ or are unknown | Document common-field selection or `ResourceMetadata = []` with no KQL; test missing titles and URL fallback |
| Sample token lacks connector permission | Document delegated `ExternalItem.Read.All`; keep token acquisition in the host |
| Work expands into connector provisioning or multi-source orchestration | Keep both outside this feature, as the approved spec requires |

## Verification commands

```sh
dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release
dotnet test --project tests/Microsoft365Retrieval.Console.Tests/Microsoft365Retrieval.Console.Tests.csproj --configuration Release
dotnet test --solution Acterion.Agents.AI.slnx --configuration Release
dotnet build Acterion.Agents.AI.slnx --configuration Release
git diff --check
```

Use the SDK pinned in `global.json` for the standard commands. If the implementation environment lacks it or a full suite stalls, record the exact limitation and the equivalent checks that completed; do not change the repository's SDK pin to make local verification pass.
