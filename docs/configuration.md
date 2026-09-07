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
| `MaximumNumberOfResults` | `int` | `8` | Must be from 1 through 25 |
| `FilterExpression` | `string?` | `null` | Optional SharePoint KQL expression |
| `ResourceMetadata` | `IReadOnlyCollection<string>` | `title`, `author` | Must be non-null and contain only nonempty values |

Options registered with `AddMicrosoft365Retrieval` are validated when resolved. Invalid result counts or metadata collections fail option validation before a Retrieval request is sent.

## Query requirements

`IMicrosoft365RetrievalClient.RetrieveAsync` accepts one natural-language query string. The client rejects:

- `null`, empty, or whitespace-only queries.
- Queries longer than 1,500 characters.

Prefer one context-rich sentence over a short generic phrase. Retrieval results are returned in API response order; do not interpret their array position as a stable ranking contract.

## Maximum results

The API supports at most 25 results. The package default is 8 to keep the amount of context passed to a model bounded.

Increasing the value can improve recall but also increases response size, model context usage, and the amount of untrusted document text your application must process. Choose the limit according to the consumer, not as an authorization or site-scoping mechanism.

## Resource metadata

The package requests `title` and `author` by default. Replace the collection when your application needs different API-supported metadata:

```csharp
options.ResourceMetadata = ["title", "author", "lastModifiedDateTime"];
```

Metadata values are exposed on each `Microsoft365RetrievalHit` as JSON elements because fields can have different shapes. Request only fields your application uses, and tolerate absent metadata.

## Typed SharePoint filters

Use `SharePointRetrievalFilter` for the supported `Path` and `SiteID` constraints. Values must come from trusted application configuration.

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

### Combine trusted scopes

```csharp
options.FilterExpression = SharePointRetrievalFilter
    .AnyOf(
        SharePointRetrievalFilter.Path(
            new Uri("https://contoso.sharepoint.com/sites/engineering/")),
        SharePointRetrievalFilter.SiteId(
            Guid.Parse("f9a9f9bc-5d23-4ed4-a960-05ba6a83bdb6")))
    .Expression;
```

`AnyOf` requires at least one non-null filter and combines all terms with logical `OR`.

## Raw filter expressions

`FilterExpression` remains available for advanced KQL scenarios outside the typed builder's narrow contract:

```csharp
options.FilterExpression = configuration["Microsoft365Retrieval:FilterExpression"];
```

Only load raw expressions from trusted application configuration. Never concatenate endpoint messages, model output, or other untrusted values into KQL.

Microsoft documents that an incorrectly formed Retrieval API filter can execute without scoping. Therefore:

- A filter must never be treated as an authorization boundary.
- Test raw filters against representative tenant content before release.
- Prefer typed filters when `Path`, `SiteID`, or their union is sufficient.
- Enforce endpoint and business authorization independently of retrieval scope.

## Configuration binding

ASP.NET Core applications can bind a section directly:

```json
{
  "Microsoft365Retrieval": {
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