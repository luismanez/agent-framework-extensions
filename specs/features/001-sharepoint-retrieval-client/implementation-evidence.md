# Implementation Evidence: Feature 001

## Public Contract Checkpoint

### Task 1: Options and Core Interfaces

- RED: `PublicContractTests` failed to compile because `IMicrosoft365RetrievalTokenProvider`, `IMicrosoft365RetrievalClient`, and `Microsoft365RetrievalHit` did not exist.
- GREEN: `PublicContractTests` passed with 2 tests after implementing the options and interface contracts.
- Extended API-surface gate: `PublicContractTests` passed with 3 tests after adding Task 2 contract assertions.
- Build: the Release solution build succeeded for the package, tests, and sample projects.

### Task 2: Immutable Results and Exception

- RED: `ResultContractTests` failed to compile because `Microsoft365RetrievalExtract`, the required `Microsoft365RetrievalHit` members, and `Microsoft365RetrievalException` did not exist.
- GREEN: `ResultContractTests` passed with 4 tests after implementing the immutable result and exception contracts.
- The tests verify copied read-only collections, response-order preservation, ordinal metadata keys, cloned string/number/Boolean/null `JsonElement` values, rejection of object and array metadata, nullable fields, and inner-exception preservation.

### Checkpoint Gates

- Full Release test project: 7 passed, 0 failed, 0 skipped.
- Full Release solution build: succeeded for all 3 projects.
- Fresh-context review: no critical, important, or blocking findings; ready for human approval of `IReadOnlyDictionary<string, JsonElement>`.
- SDK note: commands were executed with `/Users/luisman/.dotnet/dotnet` because that host provides the `10.0.300` SDK required by `global.json`; `/usr/local/bin/dotnet` exposes only `10.0.202`.

The human approval item remains open. No implementation beyond the Public Contract checkpoint has started.

## Client Behavior and Closure

**Worktree reference:** `uncommitted`
**Recorded:** 2026-09-04

| Requirement | Test or command | Observed result | Commit/worktree reference |
| --- | --- | --- | --- |
| Valid request and local validation | `Microsoft365RetrievalClientRequestTests` | Included in full suite: 56 passed, 0 failed, 0 skipped | `uncommitted` |
| Typed successful response mapping | `Microsoft365RetrievalClientResponseTests` | Included in full suite: 56 passed, 0 failed, 0 skipped | `uncommitted` |
| Safe failure translation and no retry | `Microsoft365RetrievalClientFailureTests` | Included in full suite: 56 passed, 0 failed, 0 skipped | `uncommitted` |
| Token, send, and response-read cancellation | `Microsoft365RetrievalClientCancellationTests` | Included in full suite: 56 passed, 0 failed, 0 skipped | `uncommitted` |
| Safe logging events and allowlisted dimensions | `Microsoft365RetrievalClientLoggingTests` | 4 passed, 0 failed, 0 skipped | `uncommitted` |
| DI registration and options validation | `DependencyInjectionTests` and `Microsoft365RetrievalOptionsTests` | Included in full suite: 56 passed, 0 failed, 0 skipped | `uncommitted` |
| API-surface consumer contract | `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class 'Acterion.Agents.AI.Microsoft365.Retrieval.Tests.PublicContractTests'` | 4 passed, 0 failed, 0 skipped | `uncommitted` |
| Full tenant-independent gate | `dotnet test --solution Acterion.Agents.AI.slnx --configuration Release --verbosity quiet` | 56 passed, 0 failed, 0 skipped; no warnings or errors | `uncommitted` |
| Release build and XML docs | `dotnet build Acterion.Agents.AI.slnx --configuration Release` | Succeeded with 0 warnings and 0 errors; `src/Acterion.Agents.AI.Microsoft365.Retrieval/bin/Release/net10.0/Acterion.Agents.AI.Microsoft365.Retrieval.xml` exists | `uncommitted` |
| Direct package graph | `dotnet list src/Acterion.Agents.AI.Microsoft365.Retrieval/Acterion.Agents.AI.Microsoft365.Retrieval.csproj package --include-transitive` | Direct dependencies are `Microsoft.Agents.AI 1.19.0` and `Microsoft.Extensions.DependencyInjection.Abstractions`, `Http`, `Logging.Abstractions`, and `Options` 10.0.11 | `uncommitted` |
| Retry decision | Client and handler tests | Automatic retries are deferred; one request is sent per operation and no resilience dependency was added | `uncommitted` |

### Workflow Note

Task 5 failure and cancellation tests were observed failing before implementation. Per the requested compilation-minimizing workflow, the new Task 6 and Task 7 tests were validated after their implementations rather than in a separately compiled RED pass. All normal verification uses in-memory handlers and stub token providers; no tenant credentials, network access, or Microsoft 365 license are required.

## Sensitivity Label Contract Extension

- RED: focused contract and response tests failed to compile because `Microsoft365RetrievalSensitivityLabel` and `Microsoft365RetrievalHit.SensitivityLabel` did not exist.
- GREEN: the public contract, immutable result, Graph response mapping, missing-label behavior, and Agent Framework non-projection behavior are covered by the tenant-independent suite.
- Release validation: restore and Release build succeeded without warnings; all 140 tests passed.
- Package validation: the `.nupkg` contains the assembly and XML documentation for the new public type, and the matching `.snupkg` contains the portable PDB.

## Pre-1.0 Options Validation Hardening

- Source: the Retrieval API documents a maximum `maximumNumberOfResults` value of 25 and warns that incorrect KQL can execute without scoping.
- RED: the tenant-independent suite reported 9 expected failures for direct-construction maximum validation, blank filters through DI and direct construction, and mutation of options after client construction.
- GREEN: DI and both public client constructors use one options validator; valid options and metadata names are snapshotted before use.
- Boundary coverage: result counts 1 and 25 are accepted; 0 and 26 are rejected. Null or blank metadata and non-null whitespace filters are rejected without token acquisition or HTTP.
- Release validation: restore and the `1.0.0` Release build succeeded without warnings; all 154 tests passed.
- Package validation: `Acterion.Agents.AI.Microsoft365.Retrieval.1.0.0.nupkg` contains the stable-install README, assembly, and XML docs; the matching `.snupkg` contains the portable PDB.