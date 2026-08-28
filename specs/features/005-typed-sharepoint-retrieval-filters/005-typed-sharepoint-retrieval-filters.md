# Feature 005: Typed SharePoint Retrieval Filters

**Parent specification:** [`SPEC.md`](../../SPEC/SPEC.md)  
**Origin:** [`typed-sharepoint-retrieval-filters.md`](../../../docs/ideas/typed-sharepoint-retrieval-filters.md)  
**Status:** Draft  
**Depends on:** Repository foundation and the Feature 001 options contract  
**Enables:** Safer v0.1 SharePoint filter configuration and documentation

## Objective

Provide a small, immutable, dependency-free builder for the two common SharePoint retrieval filters supported by the Microsoft 365 Copilot Retrieval API: `Path` and `SiteID`.

The builder makes trusted application configuration easier to compose without requiring consumers to write fragile KQL strings. It narrows retrieval only; it does not grant access, enforce authorization, or replace Microsoft 365 permission trimming.

## User Outcome

A .NET developer can construct a valid path or site-ID filter, combine trusted filters with `OR`, and assign the resulting expression to `Microsoft365RetrievalOptions.FilterExpression` without implementing KQL quoting or property syntax.

## Global Requirements Inherited

This feature MUST comply with the parent specification, especially:

- §8.2 through §8.5, SharePoint data source, request options, constraints, and filter security;
- §13, authorization, user isolation, and sensitive-data handling;
- §18 and §19, minimal dependencies and the .NET 10 baseline;
- §23.1, package unit testing;
- §26 through §28, documentation, security guidance, and YAGNI;
- §34, implementation working rules.

Feature 001 owns `Microsoft365RetrievalOptions.FilterExpression` and request serialization. This feature only produces a compatible string value. If this document conflicts with the parent specification, the parent specification wins.

## Scope

### In Scope

- One public immutable `SharePointRetrievalFilter` type in the existing retrieval package.
- Typed construction from an absolute HTTPS SharePoint `Uri` or a non-empty site `Guid`.
- Deterministic `OR` composition of one or more typed filters, including mixed path and site-ID terms.
- A read-only expression compatible with `Microsoft365RetrievalOptions.FilterExpression`.
- Local argument validation before an expression is created.
- Unit tests and package documentation for construction, composition, validation, and security boundaries.

### Out of Scope

- A general KQL parser, validator, or expression-tree DSL.
- `AND`, `NOT`, inequality, range, arbitrary managed-property, or free-text builders.
- Constructing filters from arbitrary end-user strings.
- Post-filtering Retrieval API results by URL or site ID.
- Authorization, permission checks, ACL evaluation, or endpoint protection.
- Replacing or restricting the existing raw `FilterExpression` option.
- OneDrive, Copilot connectors, new projects, new packages, or new dependencies.

## Functional Requirements

### Public API

The package MUST expose this public contract:

```csharp
public sealed class SharePointRetrievalFilter
{
    public string Expression { get; }

    public static SharePointRetrievalFilter Path(Uri path);

    public static SharePointRetrievalFilter SiteId(Guid siteId);

    public static SharePointRetrievalFilter AnyOf(
        params SharePointRetrievalFilter[] filters);
}
```

The type MUST NOT expose a public constructor, setter, implicit string conversion, or factory that accepts a raw KQL expression. `Expression` is the only required output contract.

Example usage:

```csharp
options.FilterExpression = SharePointRetrievalFilter
    .AnyOf(
        SharePointRetrievalFilter.Path(engineeringSite),
        SharePointRetrievalFilter.SiteId(hrSiteId))
    .Expression;
```

### Path Filters

`Path(Uri)` MUST:

- reject `null`;
- require an absolute URI using the HTTPS scheme;
- reject a URI containing user information, a query, or a fragment;
- preserve the URI's escaped absolute representation and MUST NOT decode percent-encoded characters;
- preserve path and trailing-slash semantics rather than adding or removing a trailing slash;
- produce the canonical expression `Path:"{absoluteUri}"`.

The implementation MUST use structured `System.Uri` APIs. It MUST NOT build or validate the URI using ad hoc string parsing. A URI host is not treated as an authorization boundary, and the builder MUST NOT claim that a path belongs to a particular tenant.

### Site-ID Filters

`SiteId(Guid)` MUST:

- reject `Guid.Empty`;
- format the identifier using the lowercase invariant `D` representation;
- produce the canonical expression `SiteID:"{siteId}"`.

No Graph lookup or site resolution is performed. The caller owns supplying the SharePoint site collection identifier expected by the Retrieval API.

### OR Composition

`AnyOf(...)` MUST:

- reject a `null` filter array;
- reject an empty filter array;
- reject `null` elements;
- accept path filters, site-ID filters, or a mixture of both;
- preserve caller order and duplicate terms;
- return the single expression unchanged when given one filter;
- join multiple expressions with uppercase ` OR ` and enclose the result in one pair of parentheses;
- preserve equivalent grouping when an already-composed filter is supplied.

The implementation MAY flatten internal terms to avoid redundant nested parentheses, provided the documented output for non-nested inputs remains exact and deterministic.

### Integration Contract

The generated value MUST remain an ordinary string at the Feature 001 boundary:

```csharp
Microsoft365RetrievalOptions.FilterExpression = filter.Expression;
```

Feature 001 remains responsible for option handling and JSON request serialization. This feature MUST NOT add an overload to the retrieval client, mutate options, acquire tokens, perform HTTP requests, or introduce another request model.

The raw `FilterExpression` option remains supported for advanced KQL scenarios outside this feature's narrow contract.

### Validation and Error Semantics

Invalid arguments MUST fail synchronously at the public factory boundary:

- use `ArgumentNullException` for a null `path` or null `filters` array;
- use `ArgumentException` for an invalid URI, `Guid.Empty`, an empty filter array, or a null array element.

Exception messages MUST identify the invalid category without including confidential tenant paths beyond the normal argument value behavior of the selected BCL exception constructor.

The builder MUST NOT impose the Retrieval API's 1,500-character `queryString` limit on `filterExpression`. The current official API documentation does not publish an equivalent `filterExpression` length limit. Any future local length restriction requires an authoritative source and a parent-spec update.

### Security Semantics

Documentation and API examples MUST state all of the following:

- the builder consumes trusted application values, not arbitrary end-user input;
- a generated filter narrows retrieval but is not an authorization boundary;
- incorrectly applied or ignored filtering must never expose content the delegated user cannot already access;
- Microsoft 365 permission trimming remains the authoritative content-access boundary;
- callers remain responsible for host endpoint authorization and business rules.

The feature MUST NOT be named or described as a security scope.

## Code Conventions

- Keep the implementation in the existing `Acterion.Agents.AI.Microsoft365.Retrieval` namespace and package.
- Target `net10.0` and use only BCL APIs already available to the package.
- Prefer a sealed immutable type with private construction.
- Keep KQL property names and Boolean operators canonical and deterministic.
- Do not add interfaces, DI registration, options, extension methods, or generic filter abstractions.

## Testing Strategy

Unit tests MUST cover:

- exact output for representative site, folder, and file HTTPS paths;
- preservation of trailing slashes and escaped path segments;
- rejection of relative, non-HTTPS, user-info, query, and fragment URIs;
- exact lowercase `D` formatting for site IDs and rejection of `Guid.Empty`;
- single, multiple, mixed, nested, ordered, and duplicate `AnyOf` inputs;
- null and empty argument cases with the documented exception categories;
- percent-encoded quotes and KQL-looking URI segments remaining data rather than becoming operators;
- assignment of `Expression` to `Microsoft365RetrievalOptions.FilterExpression`;
- public API surface protection against raw-expression construction or mutation.

Normal tests MUST require no tenant, credentials, network access, Microsoft 365 license, or Graph SDK.

## Commands

```powershell
dotnet build Acterion.Agents.AI.slnx --configuration Release
dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release
```

Focused test filters MUST use the xUnit v3 Microsoft Testing Platform syntax supported by .NET 10.

Example class filter:

```powershell
dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*SharePointRetrievalFilterTests"
```

## Boundaries

### Always

- Validate inputs before creating an expression.
- Generate only `Path`, `SiteID`, and `OR` syntax defined by this feature.
- Preserve deterministic output and test every public behavior.
- Describe filters as retrieval constraints, never authorization.

### Ask First

- Add another KQL property or operator.
- Add a package dependency or another public type.
- Normalize a URI beyond the behavior specified here.
- Add post-retrieval URL validation.

### Never

- Accept raw KQL or arbitrary end-user strings through the typed builder.
- Infer authorization, tenant membership, or permissions from a filter.
- Decode URI data into executable KQL syntax.
- Add a parallel retrieval request or client abstraction.

## Acceptance Criteria

- [ ] The exact public API compiles from a separate consumer or API-surface test.
- [ ] Valid HTTPS paths produce deterministic `Path:"..."` expressions without changing path or trailing-slash semantics.
- [ ] Non-empty GUIDs produce canonical lowercase `SiteID:"..."` expressions.
- [ ] One or more path and site-ID filters compose deterministically with `OR`.
- [ ] Every invalid-input category fails synchronously with the documented exception type.
- [ ] URI data cannot introduce an additional KQL term through decoding or string concatenation.
- [ ] The generated expression can be assigned directly to Feature 001 options without changing request serialization.
- [ ] Package documentation states that filtering is not authorization and that raw `FilterExpression` remains available.
- [ ] Focused tests and the full Release build pass without live credentials.

## Plan Gate Exit Evidence

- Compile the exact public API from a separate consumer project or API-surface test before finalizing implementation tasks.
- Record representative `System.Uri.AbsoluteUri` outputs for trailing slashes, Unicode, percent-encoded quotes, query strings, and fragments; the Plan MUST preserve or reject them according to this specification without custom URI parsing.
- Map each canonical expression to an official Retrieval API example or supported-property statement for `Path`, `SiteID`, and `OR`.
- Confirm through a focused test that Feature 001 serializes `filter.Expression` unchanged as `filterExpression`.
- Record that no new NuGet dependency, project, DI registration, or live-tenant CI requirement is introduced.

## Authoritative References

- Retrieval API reference and supported filter properties: https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/copilotroot-retrieval
- Retrieval API limitations and permission trimming: https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/overview
- SharePoint KQL syntax and Boolean operators: https://learn.microsoft.com/en-us/sharepoint/dev/general-development/keyword-query-language-kql-syntax-reference