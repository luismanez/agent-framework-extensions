# Typed SharePoint Retrieval Filters

**Status:** Promoted  
**Feature specification:** [`../../specs/features/005-typed-sharepoint-retrieval-filters.md`](../../specs/features/005-typed-sharepoint-retrieval-filters.md)

## Problem Statement

How might we help .NET developers scope Microsoft 365 retrieval to trusted SharePoint locations without requiring them to construct fragile KQL strings or mistake retrieval filtering for authorization?

## Recommended Direction

Add a small immutable `SharePointRetrievalFilter` builder covering the two primary scoping mechanisms supported by the Retrieval API:

- SharePoint paths represented by absolute HTTPS `Uri` values.
- SharePoint site identifiers represented by `Guid` values.

The builder generates an expression compatible with the existing `FilterExpression` option, avoiding changes to the retrieval request lifecycle. It may combine one or more typed terms using `OR`, including mixed path and site-ID terms.

Illustrative API:

```csharp
options.FilterExpression = SharePointRetrievalFilter
    .AnyOf(
        SharePointRetrievalFilter.Path(engineeringSite),
        SharePointRetrievalFilter.SiteId(hrSiteId))
    .Expression;
```

The name deliberately says **Filter**, not **Scope** or **SecurityScope**: it narrows retrieval but does not grant or enforce authorization. Microsoft 365 permission trimming for the delegated user remains the authoritative access boundary.

## Key Assumptions to Validate

- [ ] `Path` and `SiteID` cover most real SharePoint scoping needs; validate against the sample and representative integration scenarios.
- [ ] KQL values can be escaped correctly without implementing a general KQL parser; verify quotes, backslashes, Unicode, and reserved characters with focused serialization tests.
- [ ] `OR` composition is sufficient for the MVP; confirm no initial scenario requires `AND`, `NOT`, or nested expressions.
- [ ] URI normalization does not change Graph semantics; probe trailing slashes and escaped path segments against documented examples or a live opt-in test.
- [ ] The builder measurably reduces malformed-filter risk compared with raw configuration; test the API with developers unfamiliar with SharePoint KQL.

## MVP Scope

- Add one immutable, dependency-free `SharePointRetrievalFilter` public type to the existing retrieval package.
- Provide `Path(Uri)`, `SiteId(Guid)`, and `AnyOf(...)` factory methods.
- Expose the generated KQL through a read-only `Expression` property for assignment to `FilterExpression`.
- Accept only typed, application-provided values; do not provide an arbitrary-expression escape hatch in the builder.
- Validate null, empty, non-absolute, and non-HTTPS inputs before generating an expression.
- Generate deterministic expressions without applying the Retrieval API's 1,500-character `queryString` limit to `filterExpression`, for which no equivalent limit is currently documented.
- Cover escaping, composition, validation, and length limits with focused unit tests.
- Document that filters are retrieval constraints, not authorization boundaries.

## Not Doing (and Why)

- A general KQL DSL with `AND`, `NOT`, ranges, nesting, or arbitrary managed properties: this would create a parser and serialization surface disproportionate to the initial use cases.
- Construction from end-user input: typed values are intended for trusted application configuration, and user-derived KQL would weaken the safety benefit.
- Post-filtering results by URL: it can reject valid results and may create a false impression of an additional authorization boundary.
- Replacing or removing the existing raw `FilterExpression` option: advanced consumers still need access to API capabilities outside this helper's deliberately narrow scope.
- Additional data sources, packages, or dependencies: the idea is SharePoint-specific and fits in the existing package.
- Authorization or permission enforcement: Microsoft 365 permission trimming remains responsible for deciding which content the delegated user may access.

## Open Questions

- Should the final factory names be `Path` and `SiteId`, or `ForPath` and `ForSiteId` for greater call-site clarity?
- Should `AnyOf` accept a single term, or require at least two and let callers use individual filters directly?
- Which URI components, if any, should be normalized rather than preserved exactly?
- What exact KQL representation of `SiteID` should be treated as canonical after verification against official documentation and a live API probe?
- Should length validation occur only on the composed expression or also on each individual term for more actionable errors?

## Promotion

This idea was accepted and promoted to Feature 005. The feature specification is now the authoritative source for scope, public contract, acceptance criteria, and Plan gate evidence; the assumptions and open questions above remain as the original ideation record.