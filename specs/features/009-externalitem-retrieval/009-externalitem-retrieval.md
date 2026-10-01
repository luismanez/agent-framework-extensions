# Feature 009: ExternalItem Retrieval

**Parent specification:** [`SPEC.md`](../../SPEC/SPEC.md)  
**Status:** Approved; implementation planned  
**Depends on:** Features 001, 002, and 008  
**Scope:** Follow-on feature for the existing `Acterion.Agents.AI.Microsoft365.Retrieval` package

## Objective and user outcome

Let a .NET application use the Microsoft 365 Copilot Retrieval API to retrieve permission-trimmed extracts from indexed Microsoft 365 Copilot connectors (`externalItem`). A host can query the accessible connector content as a whole or limit a configured client to specific external connection IDs. The typed client and Agent Framework adapter continue to expose the same result contract and `RetrieveAsync` method.

This extends the initial SharePoint-only parent specification and Feature 008's OneDrive source selection. Their repository-wide security, dependency, testing, and packaging rules continue to apply.

## Design decisions

1. Source selection remains fixed per constructed client in `Microsoft365RetrievalOptions`. `ExternalItem` is a third enum value; `RetrieveAsync` does not acquire a source parameter.
2. `ExternalItemConnectionIds` is optional. `null` omits `dataSourceConfiguration` and queries eligible connector content accessible to the delegated user. A configured nonempty collection restricts the request to those connection IDs. An empty collection is invalid rather than silently becoming an unscoped query.
3. Connection IDs are trusted host configuration, not request text or model output. They are not an authorization boundary; Microsoft Graph's delegated permission trimming remains authoritative.
4. The first increment supports indexed `externalItem` content through the Retrieval API. It does not create connectors, ingest external items, or call federated MCP connectors directly.
5. Keep the package's default `ResourceMetadata` (`title`, `author`) for compatibility. For connector retrieval, hosts explicitly choose fields retrievable across every selected connection, or `[]` when no common fields are known. The client does not discover schemas or choose different metadata per hit.

## Scope

### In scope

- Add `ExternalItem` to the existing source enum and map it to Graph's `externalItem` value.
- Add optional external connection IDs to the existing options, validate and snapshot them, and serialize Graph's nested `dataSourceConfiguration` only when IDs are configured.
- Reuse the current endpoint, delegated token provider, query/filter/result types, exception behavior, and Agent Framework adapter.
- Document connector permissions, licensing, connection scoping, schema-dependent KQL and metadata, and troubleshooting.
- Extend the existing Console sample to select `ExternalItem`, configure optional connection IDs and connector-specific metadata names, display a URL fallback when `title` is absent, and show a `--retrieval-only` example. Update the ASP.NET Core sample guide for its existing options binding. Do not create a new sample project.
- Add tenant-independent request, response, validation, adapter, sample-configuration, and regression tests.

### Out of scope

- Creating or administering external connections, schemas, ACLs, or items; indexing, crawling, or connector provisioning.
- Direct support for federated MCP connectors or a separate Graph Search API integration.
- Simultaneous SharePoint, OneDrive, and connector retrieval; fan-out, batching, result merging, or cross-source ranking.
- A connector-specific typed KQL builder, automatic schema discovery, or local KQL syntax validation.
- App-only Graph access, credential acquisition inside the package, licensing or billing setup, and live-tenant tests in the normal suite.
- New result models, connector-specific metadata types, or changes to the public Agent Framework adapter API.

## Public contract

Add one enum value and one source-specific option:

```csharp
public enum Microsoft365RetrievalDataSource
{
    SharePoint = 0,
    OneDriveBusiness = 1,
    ExternalItem = 2,
}

public sealed class Microsoft365RetrievalOptions
{
    public Microsoft365RetrievalDataSource DataSource { get; set; }
        = Microsoft365RetrievalDataSource.SharePoint;

    public IReadOnlyCollection<string>? ExternalItemConnectionIds { get; set; }

    // Existing MaximumNumberOfResults, FilterExpression, and ResourceMetadata remain.
}
```

`IMicrosoft365RetrievalClient.RetrieveAsync(string query, CancellationToken cancellationToken = default)` remains unchanged. Example with connection scoping:

```csharp
services.AddMicrosoft365Retrieval(options =>
{
    options.DataSource = Microsoft365RetrievalDataSource.ExternalItem;
    options.ExternalItemConnectionIds = ["ContosoITServiceNowKB"];
    options.ResourceMetadata = ["title"];
    options.FilterExpression = "Label_Title:\"Corporate VPN\"";
});
```

The filter above is illustrative: `Label_Title` is valid only when every selected connector schema marks it queryable. `title` must likewise be retrievable in every selected schema. Omitting `ExternalItemConnectionIds` searches accessible connector content without a connection-ID restriction. For connections with different or unknown schemas, use only the intersection of retrievable metadata fields and queryable filter properties, or set `ResourceMetadata = []` and omit `FilterExpression`. The adapter and Console sample then use a URL-based source-name fallback.

## Functional requirements

### Configuration and request

- The default source MUST remain `SharePoint`, and existing SharePoint and OneDrive requests MUST keep their current JSON, including omission of `dataSourceConfiguration`.
- `ExternalItem` MUST serialize as `"dataSource":"externalItem"` in a `POST https://graph.microsoft.com/v1.0/copilot/retrieval` request. Each request MUST contain exactly one data source.
- When `ExternalItemConnectionIds` is `null`, the request MUST omit `dataSourceConfiguration` entirely. Do not send `externalItem: {}` or `connections: []`.
- When IDs are supplied, the request MUST contain `"dataSourceConfiguration":{"externalItem":{"connections":[{"connectionId":"..."}]}}`, preserving the supplied order and identifier text. Follow the v1.0 request example; do not require or emit `@odata.type` solely because the resource-type documentation shows it.
- A non-null collection MUST contain at least one ID. Reject null, empty, or whitespace-only entries and duplicate IDs using ordinal comparison. Do not trim, case-fold, or guess the connector ID format. Reject IDs configured for SharePoint or OneDrive rather than ignoring them.
- Invalid options MUST fail during DI resolution or direct client construction, before token acquisition or HTTP I/O. The validated ID collection MUST be copied so later mutation of the original collection cannot change a constructed client's requests.
- `FilterExpression`, `ResourceMetadata`, and `MaximumNumberOfResults` MUST retain their existing validation and forwarding behavior, except that `ResourceMetadata = []` for `ExternalItem` MUST omit the optional `resourceMetadata` request property to request no metadata. SharePoint and OneDrive JSON MUST remain unchanged. Connector filters use properties marked queryable in every selected connection schema; requested metadata uses properties retrievable in every selected connection schema. The package MUST NOT claim to validate or discover schemas, rewrite metadata per hit, or silently drop unsupported field names.
- Query length, cancellation, HTTP errors, safe logging, and response parsing MUST retain their existing behavior.

### Results and Agent Framework

- Connector hits MUST map to the existing `Microsoft365RetrievalHit`, extract, optional sensitivity-label, and metadata models. A hit may have `resourceType: "externalItem"`, missing requested metadata, and extracts without `relevanceScore`; no new result subtype is required.
- The Agent Framework adapter MUST produce `TextSearchResult` with the original `webUrl` as `SourceLink`, available extracts as `Text`, and the existing title/URL fallback as `SourceName`. It MUST NOT infer a source from the URL hostname or require a relevance score.
- Graph's result and extract order MUST be preserved; the package MUST NOT claim comparable ranking across separate source requests.

### Authentication, availability, and safety

- Calls MUST use the host-supplied delegated token for the current work or school user. Connector retrieval requires delegated `ExternalItem.Read.All`; SharePoint and OneDrive retain their separate `Files.Read.All` and `Sites.Read.All` requirements. Personal Microsoft accounts and application permissions are unsupported by this API.
- Documentation MUST explain that Copilot-licensed users can use Retrieval and that tenant-enabled pay-as-you-go consumption can cover Copilot connectors for unlicensed users under Microsoft's current availability model. The package MUST NOT detect licenses or configure billing.
- Connection IDs and KQL narrow retrieval but MUST NOT be described as authorization controls. Invalid KQL can cause Graph to execute an unscoped query; host configuration must therefore be trusted, and delegated permission trimming remains authoritative.
- The package MUST NOT log delegated tokens, connection IDs, filters, retrieved content, or metadata values, or silently switch sources after an error.

## Technical constraints and project structure

- Target the existing `net10.0` package and `v1.0` Retrieval endpoint. Use `System.Net.Http` and `System.Text.Json`; add no Graph SDK or new runtime dependency.
- Public enum/options: `src/Acterion.Agents.AI.Microsoft365.Retrieval/Retrieval/`.
- Options validation and request DTO: `src/Acterion.Agents.AI.Microsoft365.Retrieval/Internal/`.
- Tests: `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Retrieval/`, `AgentFramework/`, `PublicContract/`, and existing sample test projects.
- Consumer guides: `docs/configuration.md`, `docs/getting-started.md`, `docs/security.md`, `docs/troubleshooting.md`, and the two existing Retrieval sample READMEs.

Keep public names in PascalCase and Graph wire names in the serialization boundary. For example, `ExternalItemConnectionIds` maps to nested `connections[].connectionId` only when `DataSource` is `ExternalItem`.

## Testing strategy and commands

Offline tests with fake token providers and HTTP handlers MUST verify:

- unchanged default SharePoint and existing OneDrive request JSON;
- exact v1.0 `externalItem` request JSON with zero, one, and multiple configured connection IDs, including omission of `dataSourceConfiguration` when IDs are absent and omission of `resourceMetadata` when its configured collection is empty;
- validation of empty, blank, duplicate, and source-incompatible IDs before token or HTTP calls, both through DI and direct construction;
- snapshot behavior after mutation of the original ID collection;
- forwarding of connector KQL, metadata names, result count, Bearer token, and cancellation;
- connector responses with multiple extracts, missing relevance scores or optional fields, arbitrary scalar metadata, and unknown fields;
- Agent Framework title and URL fallbacks for connector URLs;
- Console configuration for source selection and optional connection IDs, including invalid combinations;
- a public consumer compiling against the new enum and option.

The normal suite MUST need no tenant, external connection, license, sign-in, or network access. A real-tenant smoke check MAY be recorded separately when an accessible indexed connector is available; it is not a release gate.

```bash
dotnet build Acterion.Agents.AI.slnx --configuration Release
dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release
dotnet test --project tests/Microsoft365Retrieval.Console.Tests/Microsoft365Retrieval.Console.Tests.csproj --configuration Release
```

## Boundaries

- **Always:** Preserve the SharePoint default, delegated-user isolation, connection-ID snapshots, validation before I/O, safe diagnostics, and compatibility with the existing adapter.
- **Ask first:** Add dependencies, change `RetrieveAsync`, add named clients or multi-source orchestration, change the typed SharePoint filter API, or include federated connectors.
- **Never:** Create or alter external connections from this package, log tokens or retrieved content, treat IDs or KQL as authorization, send an empty connection scope as an unscoped request, or fall back to another source on failure.

## Acceptance criteria

- [ ] Existing SharePoint and OneDrive consumers produce unchanged requests without new configuration.
- [ ] `ExternalItem` sends the documented Graph value and returns typed connector hits through the existing client.
- [ ] Omitted IDs omit `dataSourceConfiguration`; configured IDs produce the exact nested Graph request shape.
- [ ] Invalid ID configuration fails before token acquisition or network I/O, and later mutations cannot alter a constructed client.
- [ ] The Agent Framework adapter yields usable links, names, and text for connector hits with missing optional fields.
- [ ] The existing Console sample can demonstrate both unscoped and connection-scoped connector retrieval in `--retrieval-only` mode, with configurable metadata names and a useful URL fallback when no title is returned.
- [ ] Documentation covers delegated permission, connector schema requirements, licensing, scoping behavior, and the one-source-per-request constraint.
- [ ] Focused offline tests and the full Release build pass.

## Deferred follow-ups

1. A typed connector filter helper may be added if schema-specific raw KQL proves difficult to use safely.
2. Named clients and multi-source orchestration remain separate features.

## Authoritative references

- [Retrieval API request and examples](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/copilotroot-retrieval): `externalItem`, optional source configuration, connection-ID JSON, result shape, and delegated permissions.
- [External item configuration resource](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/resources/externalitemconfiguration) and [connection item resource](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/resources/connectionitem): nested connection configuration.
- [Retrieval API overview and limitations](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/overview): licensing, source isolation, optional extract scores, and KQL behavior.
- [Copilot connector models](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/overview-copilot-connector): indexed versus federated connectors.
