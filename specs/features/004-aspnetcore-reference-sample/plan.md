# Implementation Plan: Feature 004 - ASP.NET Core Reference Sample

## Overview

Build the executable `net10.0` Minimal API sample that connects an authenticated employee request to an Azure OpenAI Chat Completions agent enriched with automatic Microsoft 365 retrieval. The sample remains explicit and small: ASP.NET Core owns the HTTP and authorization boundary, Microsoft Identity Web owns delegated OBO token acquisition, a sample-local adapter implements `IMicrosoft365RetrievalTokenProvider`, Features 001 and 002 own retrieval integration, and Agent Framework owns model invocation and citation-aware context handling.

This plan is local to Feature 004. The executable checklist is [`todo.md`](todo.md), and the governing specification is [`004-aspnetcore-reference-sample.md`](004-aspnetcore-reference-sample.md).

## Planning Baseline

- Features 001 and 002 must be implemented, approved, and available to the sample before this plan starts.
- Feature 003 is an independent console reference sample and is not a prerequisite.
- The sample uses Minimal APIs and exposes only authenticated `POST /api/assistant` for the v0.1 scenario.
- Azure OpenAI Chat Completions is selected for broad model compatibility and the direct `ChatClient.AsAIAgent(...)` path required by Feature 002.
- Model authentication uses `DefaultAzureCredential` for local development. The README must recommend a deliberately selected managed or workload identity credential for production.
- Package versions are pinned centrally: `Azure.AI.OpenAI 2.1.0`, `Azure.Identity 1.21.0`, `Microsoft.Agents.AI.OpenAI 1.19.0`, and `Microsoft.AspNetCore.Mvc.Testing 10.0.11`. The Agent Framework provider stays aligned with `Microsoft.Agents.AI 1.19.0`.
- The stable 1.19.0 response path is `AgentResponse.Text`; the endpoint maps it to `{ "answer": response.Text }` without reconstructing citations.
- Host tests use `WebApplicationFactory<Program>`, test authentication, and a fake `AIAgent`; they make no Entra, Graph, SharePoint, or model-provider calls.

## Scope Boundaries

### In Scope

- Executable sample project, committed non-secret configuration, launch profile, and `Program.cs`.
- Protected bearer authentication, downstream token acquisition, in-memory token cache, and a sample-local Microsoft Identity Web token-provider adapter.
- Azure OpenAI `AzureOpenAIClient` with `DefaultAzureCredential`, `GetChatClient(deploymentName)`, and `AsAIAgent(...)`.
- Default automatic retrieval through `AIAgentBuilder.UseMicrosoft365Retrieval(..., BeforeAIInvoke)`.
- Request validation, cancellation propagation, safe Problem Details, focused host tests, sample README, and implementation evidence.

### Out of Scope

- Browser UI, persistence, deployment infrastructure, resource provisioning, interactive consent, claims-challenge handling, or business authorization.
- API-key model authentication, app-only Graph access, fallback identities, live CI tests, or structured citation extraction.
- New package abstractions introduced only to make the sample testable.

## Architecture Decisions

### 1. Minimal API and Endpoint Contract

Use a top-level `Program.cs` with `public partial class Program` for `WebApplicationFactory<Program>`. Map an authenticated `POST /api/assistant` that accepts a small request record, rejects null or whitespace `message` values with validation Problem Details, calls the registered `AIAgent.RunAsync(message, cancellationToken: requestAborted)`, and returns:

```json
{
  "answer": "..."
}
```

Use `AddProblemDetails()` and the built-in exception handler for safe host failures. Do not serialize exception details, tokens, raw retrieval responses, or retrieved documents. Authorization is attached directly with `RequireAuthorization()`.

### 2. Identity Ownership

Configure the host in this order:

1. `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(...)`;
2. `EnableTokenAcquisitionToCallDownstreamApi()` and `AddInMemoryTokenCaches()`;
3. `AddAuthorization()`;
4. `AddMicrosoft365Retrieval(configurationSection)`;
5. register the sample-local `MicrosoftIdentityWebRetrievalTokenProvider` as `IMicrosoft365RetrievalTokenProvider`.

The endpoint does not acquire or inspect tokens. The sample-local provider uses `ITokenAcquisition` to obtain the authenticated caller's delegated Graph token. The model uses a separate Azure credential and that credential must never be used for Microsoft Graph retrieval.

The adapter remains in `samples/Microsoft365Retrieval.AspNetCore`; neither it nor Microsoft Identity Web is moved into the base package.

### 3. Model and Retrieval Pipeline

Register a singleton base agent from:

```csharp
new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
    .GetChatClient(deploymentName)
    .AsAIAgent(instructions: ..., name: ...)
```

Wrap it through `new AIAgentBuilder(baseAgent).UseMicrosoft365Retrieval(options => ..., Microsoft365RetrievalBehavior.BeforeAIInvoke).Build(serviceProvider)`. The full-options overload binds maximum results, metadata fields, and the optional trusted `FilterExpression` from `Microsoft365Retrieval` configuration. No endpoint input is interpolated into KQL.

The README shows that changing only the behavior to `OnDemandFunctionCalling` enables model-directed search and explains that model-generated search queries remain untrusted input to the retrieval boundary.

### 4. Configuration and Startup

Committed `appsettings.json` contains placeholders and safe defaults only. Use the existing `AzureAd` convention for the protected API, `AzureOpenAI:Endpoint` and `AzureOpenAI:DeploymentName` for the model, and `Microsoft365Retrieval` for retrieval options. Secrets and real tenant data are supplied through user secrets, environment variables, or deployment-specific secure configuration.

Client construction is lazy with respect to network access: build and host startup must not acquire tokens or contact external services. The launch profile defines the local URL documented in the README. Invalid configuration must fail with field names but without echoing values.

### 5. Test Boundary

Create a dedicated sample integration-test project. Its custom `WebApplicationFactory<Program>` supplies valid placeholder configuration, replaces authentication with a deterministic test scheme, and replaces the registered `AIAgent` with a fake. Tests cover unauthenticated rejection, input validation, answer serialization, request cancellation, and safe failure Problem Details.

The fake agent is test-only. Production code resolves the native `AIAgent`; no sample service wrapper is added.

## Dependency Graph

```text
Features 001-002 approved
        |
        v
Task 1: package and executable-host probe
        |
        v
Task 2: protected endpoint contract
        |
        v
Task 3: identity, model, and retrieval wiring
        |
        v
Task 4: sample guidance and security review
        |
        v
Task 5: startup, full validation, and evidence
```

## Task Plan

| Task | Outcome | Size | Depends on |
| --- | --- | --- | --- |
| 1 | Prove the pinned provider graph and executable Minimal API host | M | Features 001-002 |
| 2 | Deliver the authenticated and validated endpoint against a fake native agent | M | Task 1 |
| 3 | Wire delegated identity, Azure OpenAI, and automatic retrieval explicitly | M | Task 2 |
| 4 | Document setup, modes, configuration, and all security boundaries | M | Task 3 |
| 5 | Prove startup and Release gates and record implementation evidence | S | Task 4 |

Detailed acceptance criteria, likely files, and commands are maintained in [`todo.md`](todo.md).

## Acceptance-Criteria Coverage

| Feature criterion | Planned evidence |
| --- | --- |
| Executable `net10.0` ASP.NET Core sample | Tasks 1 and 5 build/startup evidence |
| Authenticated, validated, cancellable endpoint | Task 2 host tests |
| Delegated caller identity reaches retrieval | Task 3 service-graph and sample-token-provider tests |
| Automatic retrieval with documented on-demand mode | Tasks 3 and 4 |
| Secret-free configuration and complete security guidance | Task 4 review and repository scan |
| Credential-free normal CI | Tasks 1 through 5 host tests and Release gate |
| Optional source-aware live answer | Task 5 opt-in manual evidence |

## Risks and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Provider packages resolve incompatible OpenAI dependencies | High | Compile the exact centrally pinned graph in Task 1 before endpoint work |
| Host startup triggers external authentication | High | Construct clients without network calls and prove startup using placeholders |
| Testability introduces a sample-only service layer | Medium | Replace the native `AIAgent` registration in `WebApplicationFactory` |
| Model credential is accidentally reused for Graph | High | Keep Azure model construction and the sample-local delegated provider as explicit separate registrations |
| Endpoint leaks provider or retrieval details | High | Central Problem Details handling plus negative host tests |
| Configured KQL is mistaken for authorization | High | Accept only trusted configuration and repeat the authorization warning in code guidance |
| In-memory cache guidance is copied to production | Medium | State the single-instance limitation and require a distributed production cache |

## Plan Approval

Human approval is required before Task 1 implementation. Approval accepts:

1. Minimal API and the `{ "answer": response.Text }` response contract;
2. Azure OpenAI Chat Completions with the exact package pins above;
3. `DefaultAzureCredential` for local model access, with no API-key path by default;
4. native `AIAgent` replacement in tests instead of a sample-specific abstraction;
5. automatic retrieval as the executable default and on-demand retrieval as documentation-only configuration guidance.

## Task-Level Definition of Done

A task counts as complete only when its focused test is observed RED before implementation and GREEN afterward, its Release build succeeds, it touches no more than five files, no normal test requires credentials or network access, no secret or real tenant value is committed, and its item in [`todo.md`](todo.md) is updated immediately.

## Authoritative Sources

- Feature specification: [`004-aspnetcore-reference-sample.md`](004-aspnetcore-reference-sample.md)
- Azure OpenAI Agent Framework provider: https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/model-providers/azure-openai
- Azure OpenAI Entra authentication: https://www.nuget.org/packages/Azure.AI.OpenAI/2.1.0
- Protected web API configuration: https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-app-configuration
- ASP.NET Core integration tests: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
- Restored Agent Framework 1.19.0 package contracts under the local NuGet cache