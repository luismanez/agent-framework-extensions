# Implementation Plan: Feature 008 - OneDrive Retrieval

## Overview

Add OneDrive for Business as a configured source of the existing Retrieval client. Preserve SharePoint as the default, map the two supported enum values to Graph's exact wire strings, and reuse the current response model and Agent Framework adapter. The scope is governed by [`008-onedrive-retrieval.md`](008-onedrive-retrieval.md); execution is tracked in [`todo.md`](todo.md).

This is small enough for **one implementation prompt** covering all four tasks below. Run the focused test checkpoint after the request path is complete, then the full suite and Release build once at the end. No new package, sample application, filter builder, or multi-source orchestration is planned.

## Architecture decisions

- Add `Microsoft365RetrievalDataSource` with `SharePoint` and `OneDriveBusiness`. `Microsoft365RetrievalOptions.DataSource` defaults to `SharePoint`, so existing consumers keep the same JSON.
- Validate defined enum values and snapshot the source with the existing options. Map enum values to `sharePoint` and `oneDriveBusiness` at the internal request boundary; do not serialize enum names or accept arbitrary strings.
- Keep the existing endpoint, `RetrieveAsync` signature, delegated token provider, result models, and Agent Framework adapter. Extend tests where reuse needs proof; change adapter code only if a OneDrive fixture reveals an actual failure.
- One DI registration configures one source. The Console sample remains explicitly SharePoint-focused; its guide should say so rather than implying a package limit.
- Use trusted raw KQL for OneDrive in this increment. A typed OneDrive filter and simultaneous dual-source registration remain separate possible features.

## Dependency order and tasks

| Task | Deliverable | Depends on | Expected scope |
| --- | --- | --- | --- |
| 1 | Public source option, validation, snapshot, and contract tests | Existing options contract | 5 files |
| 2 | Exact Graph request mapping and OneDrive client/adapter tests | Task 1 | Up to 5 files |
| 3 | Consumer guidance for configuration, security, licensing, and diagnosis | Task 2 | Up to 5 files |
| 4 | Sample-guide consistency and reproducible implementation evidence | Tasks 2-3 | Up to 3 files |

### Checkpoint after Task 2

Run the Retrieval package test project. Confirm the SharePoint default is unchanged, OneDrive sends `oneDriveBusiness`, invalid values fail before token/HTTP I/O, and a `driveItem` hit maps through the existing adapter. Fix failures before documentation work.

### Final checkpoint

Run the full solution tests, Release build, and `git diff --check`. Review the public API and changed files against the feature scope. Record exact commands and outcomes in `implementation-evidence.md`.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| A new enum value changes existing SharePoint requests | Assert default wire JSON and snapshot behavior with the current fake HTTP handler |
| An undefined enum value reaches Graph | Reject it in the shared options validator, including direct construction and DI resolution |
| OneDrive metadata is absent | Test `driveItem` with empty metadata and the adapter's URL source-name fallback |
| Docs imply pay-as-you-go enables OneDrive | State the OneDrive license requirement and keep SharePoint-only sample guidance explicit |
| Work expands into filtering or multiple clients | Keep raw `FilterExpression`; defer new builders and named DI registrations |

## Verification commands

```sh
dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release
dotnet test --solution Acterion.Agents.AI.slnx --configuration Release
dotnet build Acterion.Agents.AI.slnx --configuration Release
git diff --check
```
