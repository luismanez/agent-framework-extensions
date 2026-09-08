# Feature 006: Expanded SharePoint Retrieval Filters

**Parent specification:** [`SPEC.md`](../../SPEC/SPEC.md)
**Status:** Implementation complete; awaiting human sign-off
**Depends on:** Feature 005 typed SharePoint retrieval filters

## Objective

Extend `SharePointRetrievalFilter` across every SharePoint property supported by the Microsoft 365 Copilot Retrieval API while keeping the developer experience small, typed, deterministic, and difficult to misuse.

Feature 005 remains the historical MVP that introduced `Path`, `SiteId`, and `AnyOf`. This feature adds to that public contract without removing or changing existing behavior.

## Public API Additions

```csharp
public static SharePointRetrievalFilter Author(string author);
public static SharePointRetrievalFilter FileExtension(string extension);
public static SharePointRetrievalFilter FileExtensions(params string[] extensions);
public static SharePointRetrievalFilter FileName(string fileName);
public static SharePointRetrievalFilter FileType(string fileType);
public static SharePointRetrievalFilter InformationProtectionLabelId(Guid labelId);
public static SharePointRetrievalFilter LastModifiedOnOrAfter(DateTimeOffset value);
public static SharePointRetrievalFilter LastModifiedOnOrBefore(DateTimeOffset value);
public static SharePointRetrievalFilter LastModifiedBetween(DateTimeOffset from, DateTimeOffset to);
public static SharePointRetrievalFilter ModifiedBy(string modifiedBy);
public static SharePointRetrievalFilter Title(string title);
public static SharePointRetrievalFilter AllOf(params SharePointRetrievalFilter[] filters);
public static SharePointRetrievalFilter Not(SharePointRetrievalFilter filter);
```

`Path`, `SiteId`, `AnyOf`, and `Expression` remain unchanged.

## Behavior

- Factory names map directly to supported Retrieval API properties, except `FileName`, which emits the API property `Filename`.
- `FileExtension` and `FileType` accept an optional leading period, require ASCII letters or digits, and emit lowercase values.
- `FileExtensions` provides the common multi-format case and composes values with `OR`.
- Text factories trim surrounding whitespace and reject empty values, quotation marks, backslashes, wildcards, and control characters.
- Last-modified factories accept `DateTimeOffset`, emit UTC ISO 8601 values, preserve fractional-second precision, and use inclusive bounds.
- `LastModifiedBetween` rejects a start instant after the end instant.
- `AllOf`, `AnyOf`, and `Not` emit explicit grouping. Repeated `AND` or `OR` compositions flatten while mixed operators retain their grouping.

## Boundaries

- The helper is not a general KQL parser and exposes no raw-expression factory.
- Advanced KQL remains available through `Microsoft365RetrievalOptions.FilterExpression`.
- Inputs are trusted application values, not endpoint text or model output.
- Filters narrow retrieval and are not authorization boundaries. Microsoft 365 permission trimming remains authoritative.
- No dependency, request model, token flow, or HTTP behavior changes.

## Acceptance Criteria

- [x] Every supported SharePoint filter property has a typed factory.
- [x] Common multi-extension filtering requires no manual `AnyOf` construction.
- [x] Inclusive UTC date bounds and ranges serialize deterministically.
- [x] `AND`, `OR`, and `NOT` preserve logical grouping under nesting.
- [x] Existing Feature 005 expression output remains unchanged.
- [x] Invalid values fail synchronously with actionable argument exceptions.
- [x] Complex typed expressions serialize unchanged through the retrieval client.
- [x] Public documentation covers the full factory set, validation, and security boundary.
- [x] Full solution tests and Release build pass.

## Authoritative References

- Retrieval API filter properties: https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/copilotroot-retrieval
- Retrieval API limitations: https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/overview
- SharePoint KQL syntax: https://learn.microsoft.com/en-us/sharepoint/dev/general-development/keyword-query-language-kql-syntax-reference