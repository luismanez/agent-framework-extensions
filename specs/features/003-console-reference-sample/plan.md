# Feature 003 Implementation Plan: Console Reference Sample

## 1. Objective

Add a tenant-independent, buildable console sample that demonstrates host-owned delegated token acquisition with Azure Identity and consumes the Retrieval package through `IMicrosoft365RetrievalTokenProvider`.

## 2. Baseline

Before implementation:

- Features 001 and 002 provide the Retrieval and Agent Framework contracts.
- The base package compiles without an identity SDK.
- No production Azure Identity adapter exists.
- Live Microsoft 365 credentials are not available to automated tests.

## 3. Implementation sequence

### Checkpoint 1: Sample scaffold

1. Create `samples/Microsoft365Retrieval.Console` targeting `net10.0`.
2. Reference the base Retrieval project, `Microsoft.Agents.AI`, and `Azure.Identity`.
3. Add the project to `Acterion.Agents.AI.slnx`.
4. Add centrally managed `Azure.Identity` package versioning.
5. Keep the sample buildable before adding live configuration.

Verification:

```powershell
dotnet build samples/Microsoft365Retrieval.Console/Microsoft365Retrieval.Console.csproj --configuration Release
```

### Checkpoint 2: Host-owned token adapter

1. Add a sample-local `AzureIdentityRetrievalTokenProvider`.
2. Adapt `TokenCredential` to `IMicrosoft365RetrievalTokenProvider`.
3. Request only `https://graph.microsoft.com/.default`.
4. Forward cancellation.
5. Add tenant-independent tests with a fake credential when practical.

### Checkpoint 3: Device code host flow

1. Validate `AZURE_TENANT_ID` and `AZURE_CLIENT_ID`.
2. Construct `DeviceCodeCredential` explicitly.
3. Configure optional SharePoint filtering.
4. Register Retrieval and Agent Framework integration.
5. Avoid `DefaultAzureCredential`, managed identity, and application credentials.

### Checkpoint 4: Documentation

1. Add a sample README.
2. Document public-client app registration.
3. Document delegated Graph permissions and consent.
4. Document local configuration and manual execution.
5. Document Interactive Browser as an optional alternative, not an automatic fallback.
6. Explain token safety and indirect prompt-injection risk.

### Checkpoint 5: Validation

Run the canonical solution restore, build, and test commands. Confirm the base package dependency graph contains no identity SDK.

A live tenant run is a separate manual verification and MUST NOT block normal CI.

## 4. Dependency rule

`Azure.Identity` belongs to the console sample only. If several real hosts later need an identical adapter, evaluate a separate optional integration package through a new specification; do not move the adapter into the base package as part of this feature.

## 5. Exit criteria

Feature 003 is complete when every acceptance criterion in the feature specification and every item in `todo.md` is checked, except an explicitly recorded optional live-tenant verification.
