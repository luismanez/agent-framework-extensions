# Feature 003 Checklist: Console Reference Sample

## Checkpoint 1: Scaffold

- [ ] Create the `samples/Microsoft365Retrieval.Console` project.
- [ ] Add base package and Agent Framework references.
- [ ] Add the sample-only Azure Identity reference.
- [ ] Centralize the Azure Identity version.
- [ ] Add the sample to the solution.
- [ ] Build the empty sample in Release configuration.

## Checkpoint 2: Token adapter

- [ ] Implement the sample-local `AzureIdentityRetrievalTokenProvider`.
- [ ] Request the Microsoft Graph `.default` delegated scope.
- [ ] Forward cancellation.
- [ ] Never log access tokens.
- [ ] Add tenant-independent adapter tests when practical.

## Checkpoint 3: Console flow

- [ ] Validate tenant and client configuration.
- [ ] Configure `DeviceCodeCredential` explicitly.
- [ ] Register `IMicrosoft365RetrievalTokenProvider` in the host.
- [ ] Configure Microsoft 365 Retrieval.
- [ ] Configure the Agent Framework agent.
- [ ] Demonstrate automatic retrieval.
- [ ] Support optional SharePoint filter configuration.
- [ ] Do not add application-identity fallback.

## Checkpoint 4: Documentation

- [ ] Add the sample README.
- [ ] Document public-client app registration.
- [ ] Document `Files.Read.All` and `Sites.Read.All` delegated permissions.
- [ ] Document environment variables.
- [ ] Document manual execution.
- [ ] Document Interactive Browser as an optional alternative.
- [ ] Document token safety and retrieved-content risks.

## Checkpoint 5: Validation

- [ ] Restore the solution.
- [ ] Build the solution in Release configuration.
- [ ] Run tenant-independent tests.
- [ ] Confirm the base package does not reference Azure Identity, MSAL, Microsoft.Identity.Web, or ASP.NET Core.
- [ ] Optionally perform and record a live tenant run.
