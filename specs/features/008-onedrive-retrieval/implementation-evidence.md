# Implementation Evidence: Feature 008 - OneDrive Retrieval

**Date:** 2026-09-30  
**Base revision:** `c1d9df3`  
**Specification:** [`008-onedrive-retrieval.md`](008-onedrive-retrieval.md)

## Contract evidence

| Acceptance criterion | Evidence |
| --- | --- |
| SharePoint remains the default | `DataSource_DefaultsToSharePoint`, `RetrieveAsync_SendsConfiguredSharePointRequestAndReturnsEmptyHits`, and the public contract test |
| OneDrive sends the exact v1.0 request and returns typed hits | `RetrieveAsync_SendsConfiguredOneDriveRequest` and `RetrieveAsync_MapsOneDriveHitWithoutOptionalMetadata` |
| Invalid values fail before token or HTTP | DI options test and `Constructor_RejectsUnsupportedDataSourceBeforeTokenOrHttp` |
| Configured source is snapshotted | `Constructor_SnapshotsValidatedOptions` changes the original source after construction and asserts the OneDrive wire value |
| Agent Framework works with OneDrive hits | `SearchAsync_MapsOneDriveHitWithoutTitle` asserts URL fallback, extract text, link, and raw hit |
| Consumer guidance covers source and licensing | Root README, configuration, getting-started, security, troubleshooting, Entra, and sample guides |

Tests were written before request mapping. They first failed because the new public type did not exist and then because the client still sent `sharePoint` for a configured OneDrive client. The final package suite passes.

## Verification

The repository pins .NET SDK `10.0.300`, while this machine has `10.0.202`. A temporary `/private/tmp/feature008-sdk/global.json` selected the installed SDK and Microsoft Testing Platform without changing the repository's `global.json`. Each solution project was restored individually with `--ignore-failed-sources -p:NuGetAudit=false` against the existing local package cache. The full solution build then passed:

```sh
cd /private/tmp/feature008-sdk
dotnet build /Users/luisman/github/agent-framework-extensions/Acterion.Agents.AI.slnx --configuration Release --no-restore -p:NuGetAudit=false -p:UseSharedCompilation=false -m:1
```

Result: **0 warnings, 0 errors**. The installed SDK's `dotnet test` runner could not open its named pipe in the sandbox, so the freshly built xUnit v3 executables were run directly from the repository root:

| Test assembly | Passed | Failed |
| --- | ---: | ---: |
| `Acterion.Agents.AI.Microsoft365.Retrieval.Tests` | 143 | 0 |
| `Microsoft365Retrieval.Console.Tests` | 11 | 0 |
| `Microsoft365Retrieval.AspNetCore.Tests` | 6 | 0 |
| `Acterion.Agents.AI.Microsoft365.WorkContext.Tests` | 214 | 0 |
| `Microsoft365WorkContext.Console.Tests` | 16 | 0 |
| **Total** | **390** | **0** |

The ASP.NET Core test executable opened a local server and was run outside the sandbox after approval. `git diff --check` passed. No new package or project dependency was added.

## Limits

No live-tenant OneDrive call was made; the tests use fake delegated tokens and HTTP handlers. A live smoke test needs a Microsoft 365 Copilot licensed work or school account with access to suitable OneDrive content. The package does not detect that license or fall back to SharePoint.
