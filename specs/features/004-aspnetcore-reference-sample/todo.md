# Task Checklist: Feature 004 - ASP.NET Core Reference Sample

**Specification:** [`004-aspnetcore-reference-sample.md`](004-aspnetcore-reference-sample.md)
**Plan:** [`plan.md`](plan.md)
**Status:** Awaiting plan approval

Update this file after each RED-GREEN-REFACTOR cycle. A checked task must satisfy every acceptance and verification item below.

## Phase 1: Executable Host and HTTP Contract

## Task 1: Prove the Provider and Host Baseline

**Description:** Pin the selected model and host-test packages, convert the scaffold to an executable web project, and compile a minimal host that starts without contacting external services.

**Acceptance criteria:**

- [ ] Central package management pins `Azure.AI.OpenAI 2.1.0`, `Azure.Identity 1.21.0`, `Microsoft.Agents.AI.OpenAI 1.19.0`, and `Microsoft.AspNetCore.Mvc.Testing 10.0.11` without upgrading Agent Framework core.
- [ ] The sample is an executable `net10.0` Minimal API with a public partial `Program` entry point and committed placeholder configuration only.
- [ ] A startup test proves the host can build with placeholder settings without contacting Entra, Graph, SharePoint, or Azure OpenAI.

**Verification:**

- [ ] RED observed for `*SampleStartupTests` before the executable entry point exists.
- [ ] Focused startup tests pass: `dotnet test --project tests/Microsoft365Retrieval.AspNetCore.Tests/Microsoft365Retrieval.AspNetCore.Tests.csproj --configuration Release --filter-class "*SampleStartupTests"`
- [ ] Sample Release build passes: `dotnet build samples/Microsoft365Retrieval.AspNetCore/Microsoft365Retrieval.AspNetCore.csproj --configuration Release`
- [ ] Package graph is inspected: `dotnet list samples/Microsoft365Retrieval.AspNetCore/Microsoft365Retrieval.AspNetCore.csproj package --include-transitive`

**Dependencies:** Approved and implemented Features 001 and 002
**Estimated scope:** M, 5 files

**Files likely touched:**

- `Directory.Packages.props`
- `samples/Microsoft365Retrieval.AspNetCore/Microsoft365Retrieval.AspNetCore.csproj`
- `samples/Microsoft365Retrieval.AspNetCore/Program.cs`
- `samples/Microsoft365Retrieval.AspNetCore/appsettings.json`
- `tests/Microsoft365Retrieval.AspNetCore.Tests/Microsoft365Retrieval.AspNetCore.Tests.csproj`

## Task 2: Deliver the Protected Endpoint Contract

**Description:** Implement `POST /api/assistant` against the native `AIAgent` abstraction and prove authentication, validation, cancellation, successful serialization, and safe host failures with an isolated test host.

**Acceptance criteria:**

- [ ] Unauthenticated requests are challenged and null, missing, empty, or whitespace messages return validation Problem Details without invoking the agent.
- [ ] An authenticated valid request calls the agent once, passes `RequestAborted`, and returns exactly one `answer` string from `AgentResponse.Text`.
- [ ] Agent failures return safe Problem Details without tokens, exception text, raw Graph content, or retrieved documents.

**Verification:**

- [ ] RED observed for `*AssistantEndpointTests` before endpoint mapping.
- [ ] Focused endpoint tests pass: `dotnet test --project tests/Microsoft365Retrieval.AspNetCore.Tests/Microsoft365Retrieval.AspNetCore.Tests.csproj --configuration Release --filter-class "*AssistantEndpointTests"`
- [ ] Startup tests and sample Release build pass.

**Dependencies:** Task 1
**Estimated scope:** M, 4 files

**Files likely touched:**

- `samples/Microsoft365Retrieval.AspNetCore/Program.cs`
- `tests/Microsoft365Retrieval.AspNetCore.Tests/SampleWebApplicationFactory.cs`
- `tests/Microsoft365Retrieval.AspNetCore.Tests/AssistantEndpointTests.cs`
- `tests/Microsoft365Retrieval.AspNetCore.Tests/TestDoubles/StubAIAgent.cs`

## Checkpoint: Host Contract

- [ ] Tasks 1 and 2 focused tests pass together without credentials or network access.
- [ ] The endpoint returns only the specified `answer` contract or Problem Details.
- [ ] Request cancellation reaches the fake agent.
- [ ] Human confirms Minimal API and native-agent test boundaries.

## Phase 2: Identity and Retrieval Pipeline

## Task 3: Wire the Complete Agent Pipeline

**Description:** Configure protected API authentication, delegated downstream token acquisition, the Azure OpenAI Chat Completions agent, and Feature 002 automatic retrieval using explicit host registrations.

**Acceptance criteria:**

- [ ] Microsoft Identity Web configures bearer authentication, downstream token acquisition, and in-memory caching.
- [ ] A provider under `samples/Microsoft365Retrieval.AspNetCore` adapts `ITokenAcquisition` to `IMicrosoft365RetrievalTokenProvider`; no model credential is registered as a Graph token provider.
- [ ] Microsoft Identity Web and the sample provider are absent from the base package dependency graph and source tree.
- [ ] `AzureOpenAIClient` uses `DefaultAzureCredential` and `GetChatClient(deploymentName)`, decorates that client through `ChatClientBuilder.UseMicrosoft365Retrieval(..., BeforeAIInvoke)`, then creates the agent with `AsAIAgent(...)`.
- [ ] Retrieval binds maximum results, metadata fields, and optional trusted `FilterExpression` from configuration; endpoint messages never become KQL or system instructions.

**Verification:**

- [ ] RED observed for `*SampleServiceGraphTests` before complete registrations.
- [ ] Focused service-graph tests pass without resolving external tokens or making network calls.
- [ ] Endpoint tests and sample Release build pass.
- [ ] Dependency inspection shows Agent Framework packages aligned at 1.19.0.

**Dependencies:** Task 2 and approved Features 001 and 002
**Estimated scope:** M, 5 files

**Files likely touched:**

- `samples/Microsoft365Retrieval.AspNetCore/Program.cs`
- `samples/Microsoft365Retrieval.AspNetCore/Authentication/MicrosoftIdentityWebRetrievalTokenProvider.cs`
- `samples/Microsoft365Retrieval.AspNetCore/appsettings.json`
- `samples/Microsoft365Retrieval.AspNetCore/Microsoft365Retrieval.AspNetCore.csproj`
- `tests/Microsoft365Retrieval.AspNetCore.Tests/SampleServiceGraphTests.cs`

## Checkpoint: Complete Pipeline

- [ ] The production service graph contains distinct delegated Graph and Azure model credential paths.
- [ ] Automatic retrieval is the registered default behavior.
- [ ] All sample tests pass without tenant or model access.
- [ ] Release build succeeds.

## Phase 3: Guidance and Closure

## Task 4: Publish Sample Setup and Security Guidance

**Description:** Write end-to-end sample guidance for Entra, Graph consent, Azure OpenAI, configuration, execution, retrieval modes, filtering, caching, and inherited security boundaries.

**Acceptance criteria:**

- [ ] The README covers prerequisites, supported accounts/licenses, app registration and exposed scope, delegated `Files.Read.All` and `Sites.Read.All`, consent, secure OBO credentials, model settings, and authenticated request instructions.
- [ ] It shows user-secrets/environment configuration, automatic and on-demand retrieval, trusted SharePoint path filters, model-controlled on-demand queries, and the production distributed-cache requirement.
- [ ] It includes every parent section 27 boundary and states that the repository is a community sample rather than an official Microsoft package; no secret or real tenant value is present.

**Verification:**

- [ ] Documentation checklist is reviewed against every Feature 004 documentation requirement.
- [ ] Secret-pattern review passes for committed sample files.
- [ ] `git diff --check` passes for the sample README and Feature 004 documents.
- [ ] Sample tests and Release build pass after documented commands are finalized.

**Dependencies:** Task 3
**Estimated scope:** S, 2 files

**Files likely touched:**

- `samples/Microsoft365Retrieval.AspNetCore/README.md`
- `samples/Microsoft365Retrieval.AspNetCore/Properties/launchSettings.json`

## Task 5: Prove Startup and Close Feature 004

**Description:** Run the complete credential-free release gate, prove `dotnet run` reaches the documented local URL, optionally execute live validation when explicitly configured, and record reproducible evidence.

**Acceptance criteria:**

- [ ] Release restore, sample build, full solution tests, and a bounded `dotnet run` startup proof succeed without external credentials.
- [ ] Evidence maps every acceptance criterion to an observed test or command and records the exact package graph and local URL.
- [ ] Optional live validation is opt-in only and, when performed, records a grounded answer with source-aware citation text without recording confidential content.

**Verification:**

- [ ] Full tests pass: `dotnet test --solution Acterion.Agents.AI.slnx --configuration Release`
- [ ] Sample build passes: `dotnet build samples/Microsoft365Retrieval.AspNetCore/Microsoft365Retrieval.AspNetCore.csproj --configuration Release`
- [ ] Startup proof runs the documented `dotnet run --project samples/Microsoft365Retrieval.AspNetCore/Microsoft365Retrieval.AspNetCore.csproj --no-build` command and observes the launch-profile URL.
- [ ] Repository-wide `git diff --check` passes.

**Dependencies:** Task 4
**Estimated scope:** S, 2 files

**Files likely touched:**

- `samples/Microsoft365Retrieval.AspNetCore/README.md`
- `specs/features/004-aspnetcore-reference-sample/implementation-evidence.md`

## Checkpoint: Feature Complete

- [ ] Every Feature 004 acceptance criterion has recorded evidence.
- [ ] Normal tests and startup require no tenant, user, token, Graph, SharePoint, or model connection.
- [ ] No credential, token, real tenant identifier, or confidential SharePoint content is committed.
- [ ] Human review approves Feature 004 for release use.

## Plan Approval

- [ ] Approve Minimal API and `{ "answer": response.Text }`.
- [ ] Approve Azure OpenAI Chat Completions and the pinned package versions.
- [ ] Approve `DefaultAzureCredential` as the local model path with no API-key default.
- [ ] Approve native `AIAgent` replacement in host tests.
- [ ] Approve automatic retrieval as the executable default and on-demand mode as documented configuration.
- [ ] Approve this plan and authorize Task 1 implementation.