# Task Checklist: Feature 009 - ExternalItem Retrieval

**Specification:** [`009-externalitem-retrieval.md`](009-externalitem-retrieval.md)  
**Plan:** [`plan.md`](plan.md)  
**Status:** Ready for implementation

Complete these tasks in order in one implementation prompt. Check boxes only after the stated verification; continue through the final checkpoint without requesting approval between tasks.

## Task 1: Configure and validate ExternalItem

**Depends on:** None.

**Acceptance:** Consumers can select `ExternalItem` and optionally supply connection IDs. `null` permits no connection restriction; configured empty, blank, duplicate, or source-incompatible IDs fail during DI resolution and direct construction before token or HTTP calls. Construction copies the IDs, preserving order and text. SharePoint remains the default.

**Verify:** Focused options and public-contract tests cover default, valid and invalid settings, and mutation of the caller's collection after construction.

**Likely files:** `Microsoft365RetrievalDataSource.cs`; `Microsoft365RetrievalOptions.cs`; `Microsoft365RetrievalOptionsValidator.cs`; `Microsoft365RetrievalOptionsTests.cs`; `PublicContractTests.cs`.

- [ ] Add the enum value and nullable option with XML documentation.
- [ ] Validate the source/ID combination and snapshot IDs.
- [ ] Prove public API, default, invalid-input, and mutation behavior offline.

## Task 2: Send connector requests and reuse results

**Depends on:** Task 1.

**Acceptance:** One v1.0 request sends `dataSource: externalItem`; no IDs omit `dataSourceConfiguration`, while one or more IDs produce `externalItem.connections[].connectionId` without `@odata.type`. Empty `ResourceMetadata` omits `resourceMetadata` for this source. Existing SharePoint and OneDrive request JSON remains unchanged. Connector hits with missing optional scores or metadata use existing result and Agent Framework contracts.

**Verify:** Run the focused Retrieval test command in `plan.md`; inspect exact request JSON, delegated Bearer header, forwarded KQL/metadata/limit, response mapping, and adapter URL/title fallback.

**Likely files:** `RetrievalWireModels.cs`; `Microsoft365RetrievalClient.cs`; `Microsoft365RetrievalClientRequestTests.cs`; `Microsoft365RetrievalClientResponseTests.cs`; `Microsoft365RetrievalSearchTests.cs`. Update `Microsoft365RetrievalHit.cs` XML summary if it still names only SharePoint.

- [ ] Serialize the source and optional nested connection configuration.
- [ ] Cover zero, one, and multiple IDs plus SharePoint/OneDrive regressions.
- [ ] Cover connector response and adapter behavior; run the package tests.

## Checkpoint: Package behavior

- [ ] Invalid ID configuration fails before token acquisition or HTTP.
- [ ] Existing sources retain their request contract; connector request JSON matches the spec.
- [ ] Connector results reach the existing adapter without new result types.

## Task 3: Extend the existing Console sample

**Depends on:** Task 2.

**Acceptance:** Console accepts `DataSource=ExternalItem`, optional connection IDs, and connector-specific metadata names from configuration, forwards them to package options, shows a URL fallback when `title` is absent, and rejects SharePoint site URLs when another source is selected. Its `--retrieval-only` guide shows both all-accessible and connection-scoped queries. Existing SharePoint and OneDrive settings still work.

**Verify:** Console tests cover both connector configurations, metadata overrides, missing titles, and invalid combinations without sign-in; run the Console test command in `plan.md`.

**Likely files:** `samples/Microsoft365Retrieval.Console/Program.cs`; `appsettings.json`; `README.md`; `tests/Microsoft365Retrieval.Console.Tests/AzureIdentityRetrievalTokenProviderTests.cs`.

- [ ] Parse and forward source-specific IDs and requested metadata through the current configuration path.
- [ ] Test unscoped, scoped, and invalid Console configurations.
- [ ] Document delegated permission and `--retrieval-only` examples.

## Task 4: Document the consumer contract

**Depends on:** Tasks 2-3.

**Acceptance:** Package guides explain how to configure connector IDs, schema-specific queryable KQL and retrievable metadata, delegated `ExternalItem.Read.All`, current licensing/pay-as-you-go availability, and safe troubleshooting. The ASP.NET Core guide shows its existing options binding with `ExternalItemConnectionIds` and the required delegated scope.

**Verify:** Compare examples with the public API and authoritative spec references; run `git diff --check`.

**Likely files:** `docs/configuration.md`; `docs/getting-started.md`; `docs/security.md`; `docs/troubleshooting.md`; `samples/Microsoft365Retrieval.AspNetCore/README.md`.

- [ ] Update package configuration and getting-started examples.
- [ ] Document permissions, licensing, common-field selection (or empty metadata/no KQL for unknown schemas), and diagnosis.
- [ ] Add the ASP.NET Core configuration example without changing its host code.

## Checkpoint: Feature complete

- [ ] Every spec acceptance criterion has test, documentation, or recorded review evidence.
- [ ] Full solution tests, Release build, and diff hygiene pass, or exact environment limitations are recorded.
- [ ] `implementation-evidence.md` records commands, results, and any unverified live-tenant behavior.
- [ ] Changes stay within the existing Retrieval package, samples, tests, and guides.
