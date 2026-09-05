# Feature 003 Checklist: Console Reference Sample

## Checkpoint 1: Scaffold

- [x] Create the `samples/Microsoft365Retrieval.Console` project.
- [x] Add base package and Agent Framework references.
- [x] Add the sample-only Azure Identity reference.
- [x] Centralize the Azure Identity version.
- [x] Add the sample to the solution.
- [x] Build the empty sample in Release configuration.

## Checkpoint 2: Token adapter

- [x] Implement the sample-local `AzureIdentityRetrievalTokenProvider`.
- [x] Request the Microsoft Graph `.default` delegated scope.
- [x] Forward cancellation.
- [x] Never log access tokens.
- [x] Add tenant-independent adapter tests when practical.

## Checkpoint 3: Console flow

- [x] Validate tenant and client configuration.
- [x] Configure `DeviceCodeCredential` explicitly.
- [x] Register `IMicrosoft365RetrievalTokenProvider` in the host.
- [x] Configure Microsoft 365 Retrieval.
- [x] Configure the Agent Framework agent.
- [x] Demonstrate automatic retrieval.
- [x] Support optional SharePoint filter configuration.
- [x] Do not add application-identity fallback.

## Checkpoint 4: Documentation

- [x] Add the sample README.
- [x] Document public-client app registration.
- [x] Document `Files.Read.All` and `Sites.Read.All` delegated permissions.
- [x] Document environment variables.
- [x] Document manual execution.
- [x] Document Interactive Browser as an optional alternative.
- [x] Document token safety and retrieved-content risks.

## Checkpoint 5: Validation

- [ ] Restore the solution.
- [ ] Build the solution in Release configuration.
- [ ] Run tenant-independent tests.
- [x] Confirm the base package does not reference Azure Identity, MSAL, Microsoft.Identity.Web, or ASP.NET Core.
- [ ] Optionally perform and record a live tenant run.
