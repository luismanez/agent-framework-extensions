# Implementation evidence: Feature 009

## Delivered

- Added `ExternalItem` source selection and optional, validated, snapshotted connection IDs to the existing Retrieval client. The request uses the documented v1.0 `externalItem.connections[].connectionId` shape. `ResourceMetadata = []` omits the optional request property for this source.
- Reused the existing hit, extract, and Agent Framework adapter contracts for connector results, including missing metadata and extract scores.
- Extended the existing Console sample to configure connector IDs and metadata, reject incompatible settings before sign-in, and display the result URL when no title is returned. Updated the package, Console, ASP.NET Core, identity, security, and troubleshooting guides.
- Kept the SharePoint default and existing SharePoint/OneDrive request JSON unchanged in regression tests.

## Verification

The repository pins .NET SDK `10.0.300` in `global.json`; this environment has `10.0.202`. Commands ran from `/private/tmp` with absolute project paths to use the installed SDK without changing the pin. Build used `--no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false`.

| Check | Result |
| --- | --- |
| `dotnet build Acterion.Agents.AI.slnx --configuration Release` with the arguments above | Passed, zero warnings/errors |
| Direct xUnit v3 Release runner: Retrieval package | 156 passed |
| Direct xUnit v3 Release runner: Retrieval Console | 25 passed |
| Direct xUnit v3 Release runner: WorkContext package | 214 passed |
| Direct xUnit v3 Release runner: WorkContext Console | 16 passed |
| `git diff --check` | Passed |

The ASP.NET Core sample test runner discovered the suite but stalled after `Starting` while initializing its `WebApplicationFactory` host. The same stall occurred from both `/private/tmp` and the repository root; a class-filtered attempt also stalled. Those tests do not exercise the changed package or Console code, and the ASP.NET Core sample compiled in the full Release build. The test process was cancelled after the stall. The full solution test pass is therefore unverified in this environment.

No live tenant, connector, delegated sign-in, licensing, or billing smoke test was run. Such a check is optional under the spec and needs an accessible indexed connector.

## Contract review

- Offline request tests cover unscoped, one-ID, and two-ID connector requests, exact nested JSON, filter/metadata/limit forwarding, delegated Bearer token, and unchanged legacy source requests.
- Offline option tests cover invalid source/ID combinations, empty/blank/duplicate IDs, failure before token or HTTP, and collection snapshot behavior.
- Response and adapter tests cover multiple extracts, absent scores and metadata, arbitrary metadata, URL/title fallbacks, and original result order.
- Console tests cover source selection, IDs, metadata override and empty JSON array, invalid combinations, and URL fallback without authentication.
- Checked Graph's [v1.0 connector request example](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/copilotroot-retrieval), [Retrieval API overview](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/overview), and [permission reference](https://learn.microsoft.com/en-us/graph/permissions-reference).
