# Task Checklist: Feature 006 - Expanded SharePoint Retrieval Filters

**Specification:** [`006-expanded-sharepoint-retrieval-filters.md`](006-expanded-sharepoint-retrieval-filters.md)
**Plan:** [`plan.md`](plan.md)
**Status:** Implementation complete; awaiting human sign-off

## Contract and Composition

- [x] Add public-contract coverage for every new factory.
- [x] Preserve existing `Path`, `SiteId`, and flattened `AnyOf` output.
- [x] Add deterministic `AllOf` and grouped `Not` composition.
- [x] Cover null, empty, nested, duplicate, and mixed-operator cases.

## Typed Properties

- [x] Add author, file name, file extension, file type, label, modified-by, and title factories.
- [x] Add inclusive UTC last-modified lower, upper, and range factories.
- [x] Normalize common file-extension input and reject unsafe text values.
- [x] Keep raw advanced KQL outside the typed helper.

## Integration and Documentation

- [x] Verify a complex typed expression serializes unchanged in a retrieval request.
- [x] Document every supported property and logical operator.
- [x] Document input restrictions and the authorization boundary.
- [x] Update the parent specification without rewriting Feature 005 history.

## Final Validation

- [x] Filter and request serialization tests pass.
- [x] All solution test assemblies pass.
- [x] Release build succeeds with no warnings.
- [x] Repository diagnostics and `git diff --check` pass.