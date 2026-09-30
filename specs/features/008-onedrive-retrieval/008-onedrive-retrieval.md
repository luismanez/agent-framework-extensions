# Feature 008: OneDrive Retrieval

**Parent specification:** [`SPEC.md`](../../SPEC/SPEC.md)  
**Status:** Implementation complete; awaiting human review  
**Depends on:** Features 001, 002, 005, and 006  
**Scope:** Follow-on feature for the existing `Acterion.Agents.AI.Microsoft365.Retrieval` package

## Objective and user outcome

Let a .NET application configure one Retrieval client to query either SharePoint or OneDrive for Business through the Microsoft 365 Copilot Retrieval API. Existing applications continue to query SharePoint without configuration changes. A configured OneDrive client can return permission-trimmed file extracts through the existing typed client and Agent Framework adapter.

This extends the initial SharePoint-only release described by the parent specification. Its repository-wide security, dependency, testing, and packaging rules still apply; its SharePoint-only statements describe the initial release rather than a permanent limit.

## Design decisions

1. Source selection belongs to `Microsoft365RetrievalOptions`, so it is fixed for each constructed client. The caller does not choose a source on each `RetrieveAsync` call.
2. This feature targets organizational OneDrive (`oneDriveBusiness`), not personal Microsoft accounts.
3. The first increment needs direct retrieval and compatibility with the existing Agent Framework adapter. It does not need a new OneDrive-specific typed filter builder or a new sample application.
4. The host continues to supply a delegated Graph token. It is responsible for the user's license, consent, and access to the requested content.

## Scope

### In scope

- Add a supported data-source selection to the existing options and send the corresponding Graph value.
- Keep the current `RetrieveAsync` signature, endpoint, result model, token boundary, and Agent Framework integration.
- Validate and snapshot the selected source alongside the other options.
- Document OneDrive configuration, KQL scoping, permissions, licensing, and troubleshooting.
- Add tenant-independent contract, request, response, and regression tests.

### Out of scope

- Personal OneDrive accounts, app-only access, or credential acquisition inside the package.
- A request that combines SharePoint and OneDrive results, client-side fan-out, Graph batching, or cross-source ranking.
- Copilot connectors (`externalItem`) and its `dataSourceConfiguration`.
- A OneDrive-specific typed filter builder, a general KQL parser, or changes to `SharePointRetrievalFilter`.
- New response properties, thumbnails, downloads, upload, file enumeration, or a new package.
- Automatic license detection, billing configuration, or live-tenant tests in the normal test suite.

## Public contract

Add a source enum with only the two tested choices and a property on the existing options:

```csharp
public enum Microsoft365RetrievalDataSource
{
    SharePoint = 0,
    OneDriveBusiness = 1,
}

public sealed class Microsoft365RetrievalOptions
{
    public Microsoft365RetrievalDataSource DataSource { get; set; }
        = Microsoft365RetrievalDataSource.SharePoint;

    // Existing MaximumNumberOfResults, FilterExpression, and ResourceMetadata remain.
}
```

`IMicrosoft365RetrievalClient.RetrieveAsync(string query, CancellationToken cancellationToken = default)` stays unchanged. The enum names are package API names; their exact wire values are `sharePoint` and `oneDriveBusiness`. Do not expose `externalItem` until that source has its own supported contract and tests.

Example:

```csharp
services.AddMicrosoft365Retrieval(options =>
{
    options.DataSource = Microsoft365RetrievalDataSource.OneDriveBusiness;
    options.FilterExpression =
        "Path:\"https://contoso-my.sharepoint.com/personal/alex_contoso_com/Documents/\"";
});
```

The path above is illustrative, not a required URL shape. Consumers must use the canonical OneDrive path shown in the item's Details pane, not a sharing link or copied browser address. `FilterExpression` remains an optional trusted KQL string; this feature does not claim to validate KQL syntax.

## Functional requirements

### Configuration and request

- The default source MUST remain `SharePoint`, preserving existing requests and configuration files.
- `OneDriveBusiness` MUST serialize as `"dataSource":"oneDriveBusiness"` in a `POST https://graph.microsoft.com/v1.0/copilot/retrieval` request.
- `SharePoint` MUST continue to serialize as `"dataSource":"sharePoint"`.
- The request MUST contain exactly one data source. It MUST NOT send `dataSourceConfiguration` for either supported source.
- The configured `FilterExpression`, `ResourceMetadata`, and `MaximumNumberOfResults` MUST be forwarded with the same validation and serialization rules for either source. The default metadata names stay `title` and `author`; the client MUST tolerate metadata fields absent from a OneDrive response.
- An undefined enum value MUST fail options validation during DI resolution or direct client construction, before token acquisition or HTTP I/O. The validated source MUST be snapshotted so later option mutation cannot change a constructed client.
- Query length, cancellation, HTTP failures, logging, and response parsing MUST retain their existing behavior for both sources.

### Results and Agent Framework

- OneDrive hits MUST use the existing `Microsoft365RetrievalHit`, extract, sensitivity-label, and metadata models. Do not infer a source from `resourceType` or the `webUrl` hostname.
- The existing Agent Framework adapter MUST map OneDrive hits to `TextSearchResult` with `SourceLink`, `SourceName`, `Text`, and `RawRepresentation` as it does for SharePoint. If `title` is absent, its URL-based source-name fallback MUST work for a OneDrive URL.
- Neither the adapter nor the client promises an ordering or relative relevance across independent SharePoint and OneDrive requests.

### Authentication and availability

- Calls MUST use the host-supplied delegated token for the current work or school user. Microsoft documents both `Files.Read.All` and `Sites.Read.All` delegated permissions for SharePoint and OneDrive retrieval.
- Documentation MUST state that OneDrive retrieval requires a Microsoft 365 Copilot licensed user under the currently documented availability model. Retrieval API pay-as-you-go consumption does not make OneDrive available to users without that license.
- The package MUST NOT attempt to determine license state locally or fall back to SharePoint after a OneDrive error.
- KQL filters are relevance/scoping inputs, not authorization boundaries. An invalid filter can cause an unscoped request; delegated Microsoft 365 permission trimming remains authoritative.

## Technical constraints and project structure

- Target the existing `net10.0` package. Use `System.Net.Http` and `System.Text.Json`; add no Graph SDK or new package dependency.
- Public API: `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/`.
- Request DTO and validation: `src/Acterion.Agents.AI.Microsoft365.Retrieval/Internal/`.
- Tests: `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/`, `AgentFramework/`, and `PublicContract/`.
- Consumer guides: `docs/configuration.md`, `docs/getting-started.md`, `docs/security.md`, and `docs/troubleshooting.md`. Update sample guides where they currently imply SharePoint is the only possible package source.

Keep public names in PascalCase and Graph wire values in the internal serialization boundary. For example, the client maps `Microsoft365RetrievalDataSource.OneDriveBusiness` to the literal `oneDriveBusiness`; callers do not supply arbitrary source strings.

## Testing strategy and commands

Unit tests with fake token providers and HTTP handlers MUST verify:

- unchanged SharePoint default and exact OneDrive request JSON, method, endpoint, and Bearer header;
- forwarding of configured filters, metadata, and result count for OneDrive;
- invalid enum values fail before token or HTTP calls, including direct construction and DI resolution;
- source snapshot behavior after mutation of the original options object;
- OneDrive responses with `driveItem`, multiple extracts, missing metadata or optional fields, and unknown fields;
- Agent Framework mapping and URL-based source-name fallback for a OneDrive hit;
- existing cancellation, safe errors, and sensitive-data logging contracts remain valid;
- a public consumer can compile against the new enum and options property.

The normal suite MUST require no tenant, license, credentials, or network access. A real-tenant smoke check MAY be recorded separately when a suitable licensed test account exists; it is not a release gate for this feature.

```bash
dotnet build Acterion.Agents.AI.slnx --configuration Release
dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release
```

## Boundaries

- **Always:** Preserve SharePoint default behavior, delegated-user isolation, validation before I/O, cancellation, safe diagnostics, and tests for the observable contract.
- **Ask first:** Add a dependency, change the `RetrieveAsync` signature, add simultaneous multi-source orchestration, introduce named DI clients, or change typed filter APIs.
- **Never:** Log tokens or retrieved content, treat KQL as authorization, claim OneDrive pay-as-you-go availability, or silently query a different source after failure.

## Acceptance criteria

- [x] Existing consumers make unchanged SharePoint requests without setting `DataSource`.
- [x] Setting `DataSource = OneDriveBusiness` sends the exact documented Graph wire value and returns typed OneDrive hits.
- [x] Unsupported enum values fail before token acquisition or network I/O.
- [x] The Agent Framework adapter can use a OneDrive client without a new adapter API and produces usable source links and names when optional metadata is absent.
- [x] Documentation explains source selection, delegated permissions, OneDrive licensing, canonical paths, and the one-source-per-request constraint.
- [x] Focused tests and the full Release build pass offline.

## Deferred follow-ups

1. A source-specific typed OneDrive filter builder may be added if raw KQL proves insufficient for consumers.
2. A later feature may provide separately named clients for both sources in one DI container. This feature configures one source per client.

## Authoritative references

- [Retrieval API request contract](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/copilotroot-retrieval): v1.0 endpoint, `oneDriveBusiness` value, permissions, and shared filter properties.
- [Retrieval API overview and limitations](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/overview): one source per request, KQL behavior, licensing, and limits.
- [Pay-as-you-go availability](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/paygo-retrieval): OneDrive exclusion.
