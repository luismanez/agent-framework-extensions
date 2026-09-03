# Feature 004: ASP.NET Core Reference Sample

**Parent specification:** [`SPEC.md`](../../SPEC/SPEC.md)
**Status:** Draft
**Depends on:** Features 001, 002, and 003
**Enables:** v0.1 release documentation and end-to-end validation

## Objective

Deliver a small, production-inspired ASP.NET Core sample that demonstrates the complete supported path from an authenticated employee request to a Microsoft Agent Framework response grounded in permission-trimmed SharePoint content.

The sample is executable guidance, not another framework layer. It should expose the security and authentication boundaries clearly while remaining small enough to read end to end.

## User Outcome

After configuring an Entra app registration, Microsoft 365 prerequisites, and a supported model endpoint, a developer can run the sample, call authenticated `POST /api/assistant`, and receive an agent response that can cite SharePoint sources retrieved under the caller's delegated identity.

## Global Requirements Inherited

This feature MUST comply with the parent specification, especially:

- §10, both Agent Framework retrieval behaviors;
- §12 and §13, identity, authorization, isolation, and prompt-injection boundaries;
- §16 and §17, sample scenario and configuration;
- §19, .NET 10 baseline;
- §23.3, sample validation;
- §26 and §27, documentation and security considerations;
- §30, expected developer experience.

Features 001 through 003 define the package contracts used by the sample. If this document conflicts with the parent specification, the parent specification wins.

## Scope

### In Scope

- An ASP.NET Core `net10.0` web API under `samples/Microsoft365Retrieval.AspNetCore`.
- Microsoft Entra ID bearer authentication through Microsoft Identity Web.
- downstream delegated token acquisition/OBO and an in-memory token cache for sample simplicity;
- package registration and optional trusted SharePoint KQL scoping from configuration;
- creation of a model-provider-backed Agent Framework agent with `TextSearchProvider`;
- authenticated `POST /api/assistant` accepting a message and returning the agent result;
- default demonstration of automatic retrieval;
- documentation showing how to switch to on-demand retrieval;
- configuration templates, user-secrets/environment-variable guidance, and a sample-specific README;
- compile-time validation that can run without credentials, with optional manual live validation.

### Out of Scope

- A browser UI, chat history store, database, custom token cache, or production deployment infrastructure.
- Provisioning Entra, Microsoft 365, SharePoint, Microsoft Foundry, or Azure OpenAI resources.
- Shipping credentials, tenant identifiers, client secrets, tokens, or real SharePoint URLs.
- Implementing claims-challenge responses, interactive consent, business authorization, or tool authorization.
- Live-tenant tests required by CI.
- Making the package depend on the sample's model provider.

## Functional Requirements

### Endpoint Contract

The sample MUST expose:

```http
POST /api/assistant
Content-Type: application/json

{
  "message": "What is our remote work policy?"
}
```

The endpoint MUST:

- require an authenticated caller;
- reject a missing, null, empty, or whitespace-only message with a client error;
- pass request cancellation through agent execution and retrieval;
- return a successful JSON response containing the Agent Framework answer text;
- use consistent ASP.NET Core problem details for invalid requests or safe host-level failures;
- avoid returning access tokens, internal exception details, raw Graph bodies, or raw retrieved documents.

The v0.1 response contract MUST contain an `answer` string. Citations are requested through `TextSearchProvider` and, when produced by the model, remain embedded in that answer using the source names and links from Feature 002. The sample MUST NOT promise a separate structured citation collection, rerun retrieval to reconstruct one, or invent a citation-extraction layer.

### Authentication and OBO

The host MUST configure Microsoft Identity Web as a protected web API, enable downstream token acquisition, and explicitly use the Feature 003 integration.

The endpoint's authenticated delegated user MUST be the identity used for Retrieval API calls. The sample MUST NOT use a daemon credential, managed identity, API key, or application token for Microsoft Graph.

An in-memory token cache MAY be used in the sample. Its README MUST state that multi-instance production hosts require an appropriate distributed cache.

### Retrieval and Agent Configuration

The sample MUST configure Feature 001 options from the `Microsoft365Retrieval` configuration section.

`FilterExpression` MAY contain a trusted application-configured SharePoint path. The sample MUST NOT build KQL from the endpoint message and MUST document that filtering is not authorization.

The default executable path MUST attach retrieval through `AIAgentBuilder.UseMicrosoft365Retrieval` using `BeforeAIInvoke`, without resolving `Microsoft365RetrievalSearch` or constructing `TextSearchProvider` manually. The README MUST show the full-options overload and the minimal change for `OnDemandFunctionCalling`, and explain that the model controls search queries in that mode.

Retrieved content MUST remain context data supplied through Agent Framework, not system instructions.

### Model Provider

The package remains model-provider independent. The sample SHOULD use a current stable Azure OpenAI or Microsoft Foundry integration with Entra-based authentication such as `DefaultAzureCredential` for local development and an appropriately constrained production credential.

The exact provider and stable package versions MUST be selected and documented during the Plan gate. API keys SHOULD NOT be the default path.

### Configuration

Committed configuration MAY contain placeholders and non-secret defaults only. Secrets MUST be supplied through user secrets, environment variables, or deployment-specific secure configuration.

At minimum, configuration documentation MUST cover:

- Entra authority/tenant and host API client identifier;
- a secure host credential required for OBO, with certificates or federated credentials preferred for production;
- model endpoint and deployment/model name;
- maximum retrieval results, metadata fields, and optional trusted filter expression.

## Documentation Requirements

The sample-specific README MUST include:

- prerequisites and supported account/license model;
- Entra app registration and exposed host API scope;
- delegated Graph permissions `Files.Read.All` and `Sites.Read.All` and consent implications;
- local configuration using user secrets or environment variables;
- run and authenticated request instructions;
- automatic and on-demand retrieval configuration;
- SharePoint path-filter guidance;
- production token-cache guidance;
- all security considerations inherited from parent §27;
- a clear statement that this is a community sample, not an official Microsoft package.

## Code Conventions

- Prefer a small Minimal API unless the endpoint contract becomes clearer with a controller.
- Keep endpoint binding, agent construction, and package registration explicit.
- Use built-in ASP.NET Core validation/problem-details capabilities before adding dependencies.
- Do not add application layers, persistence abstractions, or a custom agent service without demonstrated need.

## Testing Strategy

The sample MUST support release-level CI performing these checks:

- restore and compile the sample in Release;
- run package unit tests without tenant credentials;
- require no network calls to Entra, Graph, SharePoint, or the model provider.

Focused host tests SHOULD cover request validation, authentication enforcement, cancellation, and safe error responses using test doubles where practical.

Optional live validation MUST be explicitly enabled through environment variables or user secrets and MUST never run in normal CI.

## Commands

```powershell
dotnet build samples/Microsoft365Retrieval.AspNetCore/Microsoft365Retrieval.AspNetCore.csproj --configuration Release
dotnet run --project samples/Microsoft365Retrieval.AspNetCore/Microsoft365Retrieval.AspNetCore.csproj
dotnet test --solution Acterion.Agents.AI.slnx --configuration Release
```

The final README MUST document the actual local URL emitted by the sample launch profile rather than assuming a port in this specification.

The existing repository scaffold temporarily declares the sample as a library because no `.cs` files were created during foundation work. Feature 004 implementation MUST add the entry point and change it to an executable web project before the `dotnet run` acceptance criterion is evaluated.

## Boundaries

### Always

- Require endpoint authentication and use that caller's delegated identity for retrieval.
- Keep secrets outside committed configuration.
- Show automatic retrieval as the default and document on-demand retrieval.
- Keep package code independent of the chosen model provider.

### Ask First

- Add a browser UI, persistent storage, deployment infrastructure, or live CI tests.
- Introduce API-key authentication for the model provider.
- Add a sample-only abstraction to package code.

### Never

- Commit credentials, tokens, tenant-specific secrets, or real confidential content.
- Use app-only Graph permissions or a fallback identity.
- Treat KQL filtering as endpoint or document authorization.
- Return raw retrieved documents or authentication internals from the endpoint.

## Acceptance Criteria

- [ ] The sample builds as an executable ASP.NET Core `net10.0` application.
- [ ] `POST /api/assistant` is authenticated, validates input, and propagates cancellation.
- [ ] The caller's delegated identity flows through Feature 003 to Feature 001.
- [ ] The agent uses Feature 002 with automatic retrieval and the README documents on-demand mode.
- [ ] Configuration and documentation contain no secrets and cover all required security boundaries.
- [ ] Normal CI compiles the sample without live tenant or model credentials.
- [ ] Optional manual validation can demonstrate a grounded answer whose answer text includes source-aware citations when the model follows the Agent Framework citation prompt.

## Plan Gate Exit Evidence

- Compile one minimal Entra-first probe for each viable stable model integration, then select the option with the fewest sample-only dependencies and no API-key requirement.
- Verify the stable Agent Framework response API and bind it to the specified `{ "answer": "..." }` contract without structured citation extraction.
- Prefer Minimal API. Select a controller only if a focused authentication/validation test demonstrates a material clarity or testability advantage.
- Prove `dotnet run` starts the sample after changing its output type to executable and adding the entry point.
