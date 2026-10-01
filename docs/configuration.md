# Configuration reference

`Microsoft365RetrievalOptions` controls the request sent to the Microsoft 365 Copilot Retrieval API. Configure it when registering the package:

```csharp
services.AddMicrosoft365Retrieval(options =>
{
    options.MaximumNumberOfResults = 8;
    options.ResourceMetadata = ["title", "author"];
    options.FilterExpression = SharePointRetrievalFilter
        .Path(new Uri("https://contoso.sharepoint.com/sites/engineering/"))
        .Expression;
});
```

## Retrieval options

| Property | Type | Default | Rules |
| --- | --- | --- | --- |
| `DataSource` | `Microsoft365RetrievalDataSource` | `SharePoint` | `SharePoint`, `OneDriveBusiness`, or `ExternalItem` |
| `ExternalItemConnectionIds` | `IReadOnlyCollection<string>?` | `null` | Only for `ExternalItem`; `null` queries accessible connections, otherwise supply distinct nonblank IDs |
| `MaximumNumberOfResults` | `int` | `8` | Must be from 1 through 25 |
| `FilterExpression` | `string?` | `null` | Must be null or contain a non-whitespace KQL expression for the selected source |
| `ResourceMetadata` | `IReadOnlyCollection<string>` | `title`, `author` | Must be non-null and contain only nonempty values |

Options registered with `AddMicrosoft365Retrieval` are validated when resolved. The public `Microsoft365RetrievalClient` constructors apply the same validation, so direct construction cannot bypass these rules. Invalid options fail before token acquisition or a Retrieval request.

The client snapshots validated option values, the metadata collection, and connection IDs during construction. Later mutations to the source `Microsoft365RetrievalOptions` instance do not change an existing client; construct or resolve a new client to apply new configuration.

## Data source

The default `SharePoint` source preserves existing behavior. To retrieve organizational OneDrive content, configure the client with `OneDriveBusiness`:

```csharp
services.AddMicrosoft365Retrieval(options =>
{
    options.DataSource = Microsoft365RetrievalDataSource.OneDriveBusiness;
    options.FilterExpression =
        "Path:\"https://contoso-my.sharepoint.com/personal/alex_contoso_com/Documents/\"";
});
```

The URL is illustrative. For a real filter, copy the canonical file or folder path from OneDrive's **Details** pane, not a sharing link or browser address. The package validates that the expression is nonblank but does not parse KQL. A malformed expression can execute without the intended scope; keep authorization independent of filtering.

Each client uses one configured source, and each Retrieval API request queries one source. This package does not merge results across sources or register named clients. OneDrive retrieval requires a Microsoft 365 Copilot license for the calling user; Retrieval API pay-as-you-go does not enable OneDrive. SharePoint and OneDrive use delegated `Files.Read.All` and `Sites.Read.All` permissions. See the [Retrieval API overview](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/overview).

### Copilot connector items

Select indexed connector content with `ExternalItem`:

```csharp
services.AddMicrosoft365Retrieval(options =>
{
    options.DataSource = Microsoft365RetrievalDataSource.ExternalItem;
    options.ExternalItemConnectionIds = ["ContosoIT", "ContosoHR"];
    options.ResourceMetadata = [];
});
```

Omit `ExternalItemConnectionIds` to search connector items accessible to the delegated user without restricting connection IDs. Supplying IDs sends Graph's `dataSourceConfiguration.externalItem.connections` array in the configured order; an empty collection is rejected rather than broadening the query. IDs are trusted host settings and query scope, not an authorization control. The delegated user must have `ExternalItem.Read.All`, and Graph still trims results according to item permissions. This feature queries indexed connectors; it does not provision connections or query federated MCP connectors directly.

Connector schemas can expose different fields. Request metadata only when the field is marked retrievable in **every** selected connection, and use KQL properties only when they are queryable in every selected connection. If the common schema is unknown, use `ResourceMetadata = []` and omit `FilterExpression`; for `ExternalItem`, the empty collection omits the optional `resourceMetadata` property. The package does not discover schemas or change fields per result. The default `ResourceMetadata` remains `["title", "author"]` for compatibility, so explicitly override it for connector content. A missing title uses the result URL as the Agent Framework source name.

## Query requirements

`IMicrosoft365RetrievalClient.RetrieveAsync` accepts one natural-language query string. The client rejects:

- `null`, empty, or whitespace-only queries.
- Queries longer than 1,500 characters.

Prefer one context-rich sentence over a short generic phrase. Retrieval results are returned in API response order; do not interpret their array position as a stable ranking contract.

## Maximum results

The API supports at most 25 results. The package default is 8 to keep the amount of context passed to a model bounded.

Both boundary values, 1 and 25, are valid. Values outside that inclusive range fail locally and are never sent to Microsoft Graph.

Increasing the value can improve recall but also increases response size, model context usage, and the amount of untrusted document text your application must process. Choose the limit according to the consumer, not as an authorization or site-scoping mechanism.

## Resource metadata

The package requests `title` and `author` by default. Replace the collection when your application needs different API-supported metadata:

```csharp
options.ResourceMetadata = ["title", "author", "lastModifiedDateTime"];
```

Metadata values are exposed on each `Microsoft365RetrievalHit` as JSON elements because fields can have different shapes. Request only fields your application uses, and tolerate absent metadata.

## Sensitivity labels

When Microsoft Graph returns Microsoft Purview label information for a result, the package exposes it as `Microsoft365RetrievalHit.SensitivityLabel`. The immutable `Microsoft365RetrievalSensitivityLabel` contains nullable `SensitivityLabelId`, `DisplayName`, `ToolTip`, `Priority`, and `Color` properties.

Sensitivity labels are response metadata and are independent of the configured `ResourceMetadata` collection. `SensitivityLabel` is `null` when Graph omits the object, and individual properties can also be `null` in a partial response. The package does not infer a label from requested metadata or require the identifier to have a particular format.

The Agent Framework adapter retains the complete hit in `TextSearchResult.RawRepresentation`. It does not copy sensitivity-label values into `Text`, `SourceName`, or other model-visible fields.

## Typed SharePoint filters

`SharePointRetrievalFilter` covers every SharePoint property supported by the Retrieval API and composes them without requiring handwritten KQL. It remains a SharePoint-specific helper; this feature does not add a typed OneDrive or connector filter. Use trusted application configuration for any source.

| Retrieval property | Typed factory |
| --- | --- |
| `Author` | `Author(string)` |
| `FileExtension` | `FileExtension(string)` or `FileExtensions(params string[])` |
| `Filename` | `FileName(string)` |
| `FileType` | `FileType(string)` |
| `InformationProtectionLabelId` | `InformationProtectionLabelId(Guid)` |
| `LastModifiedTime` | `LastModifiedOnOrAfter`, `LastModifiedOnOrBefore`, or `LastModifiedBetween` |
| `ModifiedBy` | `ModifiedBy(string)` |
| `Path` | `Path(Uri)` |
| `SiteID` | `SiteId(Guid)` |
| `Title` | `Title(string)` |

### Filter by path

```csharp
options.FilterExpression = SharePointRetrievalFilter
    .Path(new Uri("https://contoso.sharepoint.com/sites/engineering/"))
    .Expression;
```

`Path` requires an absolute HTTPS URI without user information, a query string, or a fragment. Use the canonical path shown in SharePoint item or folder details rather than a sharing link.

### Filter by site ID

```csharp
options.FilterExpression = SharePointRetrievalFilter
    .SiteId(Guid.Parse("f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6"))
    .Expression;
```

The site ID must be a nonempty GUID.

### Compose filters

```csharp
options.FilterExpression = SharePointRetrievalFilter
    .AllOf(
        SharePointRetrievalFilter.SiteId(
            Guid.Parse("f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6")),
        SharePointRetrievalFilter.FileExtensions("pdf", "docx", "pptx"),
        SharePointRetrievalFilter.LastModifiedOnOrAfter(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
        SharePointRetrievalFilter.Not(
            SharePointRetrievalFilter.Title("Draft")))
    .Expression;
```

`AllOf` combines filters with `AND`, `AnyOf` combines them with `OR`, and `Not` excludes a filter. Nested expressions retain their logical grouping, while repeated uses of the same operator are flattened into deterministic output.

Date factories accept `DateTimeOffset`, compare inclusively, and emit UTC ISO 8601 values. `FileExtension` and `FileType` accept an optional leading period and normalize ASCII letters to lowercase.

Text factories trim surrounding whitespace and reject empty values, quotation marks, backslashes, wildcards, and control characters. These restrictions prevent values from changing the generated KQL structure. Use a reviewed raw expression when intentional wildcard or other advanced KQL behavior is required.

## Raw filter expressions

`FilterExpression` remains available for advanced KQL scenarios outside the typed builder's narrow contract:

```csharp
options.FilterExpression = configuration["Microsoft365Retrieval:FilterExpression"];
```

A raw expression must contain at least one non-whitespace character. Use `null` to omit filtering. This catches an obviously empty configuration but does not parse or prove the correctness of arbitrary KQL.

Only load raw expressions from trusted application configuration. Never concatenate endpoint messages, model output, or other untrusted values into KQL. For connector items, property names and queryability depend on the selected connection schemas.

Microsoft documents that an incorrectly formed Retrieval API filter can execute without scoping. Therefore:

- A filter must never be treated as an authorization boundary.
- Test raw filters against representative tenant content before release.
- For SharePoint, prefer typed filters whenever their supported properties and operators are sufficient.
- Enforce endpoint and business authorization independently of retrieval scope.

## Configuration binding

ASP.NET Core applications can bind a section directly:

```json
{
  "Microsoft365Retrieval": {
    "DataSource": "OneDriveBusiness",
    "MaximumNumberOfResults": 8,
    "ResourceMetadata": ["title", "author"],
    "FilterExpression": null
  }
}
```

```csharp
services.AddMicrosoft365Retrieval(options =>
{
    configuration.GetSection("Microsoft365Retrieval").Bind(options);
    options.FilterExpression = string.IsNullOrWhiteSpace(options.FilterExpression)
        ? null
        : options.FilterExpression;
});
```

The normalization above intentionally treats an empty configuration value as an omitted filter. Without that normalization, a blank configured value fails options validation.

Use `"SharePoint"` or omit `DataSource` for the existing default. Undefined enum values fail options validation when the client is resolved.

For connector content, the same binding supports `"DataSource": "ExternalItem"`, `"ExternalItemConnectionIds": ["ContosoIT"]`, and `"ResourceMetadata": []`. Omit `ExternalItemConnectionIds` entirely for an unrestricted connector query; do not set it to `[]`.

Use environment-variable double underscores for hierarchical keys, for example `Microsoft365Retrieval__MaximumNumberOfResults=8`.

Do not store tokens, client secrets, or certificates in this section. Retrieval options are request behavior, not identity configuration.

## Service registrations

`AddMicrosoft365Retrieval` registers:

- A named `HttpClient` with `https://graph.microsoft.com/` as its base address.
- `IMicrosoft365RetrievalClient` as transient.
- The Agent Framework retrieval adapter as transient.

The host must register exactly one usable `IMicrosoft365RetrievalTokenProvider`. Choose its lifetime according to the underlying authentication implementation. A provider that resolves the current ASP.NET Core request can be singleton only when it accesses request state through `IHttpContextAccessor` or another safe indirection, as the reference sample does.

## Agent Framework options

The extension accepts either a retrieval timing value or complete `TextSearchProviderOptions`:

```csharp
chatClientBuilder.UseMicrosoft365Retrieval(
    TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke);
```

```csharp
TextSearchProviderOptions options = new()
{
    SearchTime = TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling,
};

chatClientBuilder.UseMicrosoft365Retrieval(options);
```

Use `BeforeAIInvoke` for predictable retrieval on every model call. Use `OnDemandFunctionCalling` when the model should control tool invocation, and create the agent with `UseProvidedChatClientAsIs = true` as described in [getting started](getting-started.md#on-demand-agent-retrieval).
