# Task Checklist: Feature 003 - Microsoft Identity Web Integration

**Specification:** [`003-microsoft-identity-web-integration.md`](003-microsoft-identity-web-integration.md)
**Plan:** [`plan.md`](plan.md)
**Status:** Awaiting plan approval

Update this file after each RED-GREEN-REFACTOR cycle. A checked task must satisfy every acceptance and verification item below.

## Phase 1: Contract and Provider

## Task 1: Pin Identity Integration Contracts

**Description:** Compile the exact token-provider implementation and explicit DI extension API while recording the pinned Identity Web acquisition and cancellation surface.

**Acceptance criteria:**

- [ ] The provider implements Feature 001's exact token contract and depends on `ITokenAcquisition`, not `HttpContext` or MSAL internals.
- [ ] One scope definition contains exactly `Files.Read.All` and `Sites.Read.All`.
- [ ] The public API exposes only `AddMicrosoft365RetrievalMicrosoftIdentityWeb()` for this integration and documents the lack of in-flight cancellation in Identity Web 4.14.2.

**Verification:**

- [ ] RED observed for `*MicrosoftIdentityWebPublicContractTests`.
- [ ] Focused tests pass: `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*MicrosoftIdentityWebPublicContractTests"`
- [ ] Release build passes: `dotnet build Acterion.Agents.AI.slnx --configuration Release`

**Dependencies:** Approved Feature 001 public contract
**Estimated scope:** S, 3 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/MicrosoftIdentityWebRetrievalTokenProvider.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalIdentityWebServiceCollectionExtensions.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/MicrosoftIdentityWebPublicContractTests.cs`

## Task 2: Implement Token Acquisition Behavior

**Description:** Delegate token acquisition to Microsoft Identity Web while preserving exact scopes, original failures, and deterministic cancellation checks.

**Acceptance criteria:**

- [ ] Successful acquisition requests the exact two scopes once and returns the exact non-empty token.
- [ ] Pre-cancellation avoids acquisition; cancellation during acquisition is observed after completion and discards the token.
- [ ] Identity Web exceptions retain their original type; empty token output fails safely without exposing sensitive values.

**Verification:**

- [ ] RED observed for `*MicrosoftIdentityWebRetrievalTokenProviderTests`.
- [ ] Focused tests pass: `dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*MicrosoftIdentityWebRetrievalTokenProviderTests"`
- [ ] Public-contract tests and Release build pass.

**Dependencies:** Task 1
**Estimated scope:** M, 3 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/MicrosoftIdentityWebRetrievalTokenProvider.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/MicrosoftIdentityWebRetrievalTokenProviderTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/StubTokenAcquisition.cs`

## Checkpoint: Token Boundary

- [ ] Tasks 1 and 2 focused tests pass together.
- [ ] Release build succeeds.
- [ ] No token, assertion, claim, tenant detail, or Identity Web exception text is copied into package diagnostics.
- [ ] Human confirms exception preservation and cancellation limitations.

## Phase 2: Registration and Host Contract

## Task 3: Add Explicit DI Integration

**Description:** Register the Identity Web provider without configuring or replacing host authentication, authorization, cache, HTTP, or unrelated services.

**Acceptance criteria:**

- [ ] Explicit opt-in resolves the expected provider when no custom retrieval token provider exists.
- [ ] Repeated registration is idempotent and a pre-registered custom retrieval token provider remains authoritative.
- [ ] Missing `ITokenAcquisition` fails with actionable standard DI context and existing host auth descriptors remain unchanged.

**Verification:**

- [ ] RED observed for `*MicrosoftIdentityWebDependencyInjectionTests`.
- [ ] Focused DI tests pass.
- [ ] Provider tests and Release build pass.

**Dependencies:** Task 2
**Estimated scope:** M, 4 files

**Files likely touched:**

- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Microsoft365RetrievalIdentityWebServiceCollectionExtensions.cs`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/Acterion.Agents.AI.Microsoft365.Retrieval.csproj`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/MicrosoftIdentityWebDependencyInjectionTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/TestDoubles/StubTokenAcquisition.cs`

## Task 4: Document and Verify the Host Contract

**Description:** Add consumer guidance for protected web APIs and encode security invariants in tests without taking ownership of host setup.

**Acceptance criteria:**

- [ ] Documentation shows host-owned bearer authentication, downstream token acquisition, cache registration, Retrieval registration, and explicit Identity Web opt-in in order.
- [ ] Documentation covers work/school delegated accounts, both Graph permissions, consent/challenges, endpoint authorization, and distributed production caching.
- [ ] Negative tests prove token and representative sensitive claim values are absent from package messages and any captured logs.

**Verification:**

- [ ] RED observed for the security cases in provider and DI tests before guidance is finalized.
- [ ] Focused Identity Web test classes pass together.
- [ ] `git diff --check` passes for package guidance and Feature 003 documents.
- [ ] Release build passes.

**Dependencies:** Task 3
**Estimated scope:** M, 4 files

**Files likely touched:**

- `README.md`
- `src/Acterion.Agents.AI.Microsoft365.Retrieval/MicrosoftIdentityWebRetrievalTokenProvider.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/MicrosoftIdentityWebRetrievalTokenProviderTests.cs`
- `tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/MicrosoftIdentityWebDependencyInjectionTests.cs`

## Checkpoint: Host Integration

- [ ] All Identity Web focused tests pass without credentials or network access.
- [ ] Existing authentication and authorization service descriptors remain unchanged.
- [ ] Documentation does not imply that the package handles consent, challenges, endpoint authorization, or production cache selection.
- [ ] Release build succeeds.

## Phase 3: Closure

## Task 5: Record Evidence and Close Feature 003

**Description:** Run the complete feature gate and record evidence for the Plan Gate, acceptance criteria, security boundary, and package graph.

**Acceptance criteria:**

- [ ] Every criterion maps to a focused test or command with observed result and commit/worktree reference.
- [ ] Evidence records the exact Identity Web overload, absence of cancellation support, selected DI API, and preserved exception behavior.
- [ ] The package graph contains no new authentication package or duplicate Identity Web version.

**Verification:**

- [ ] Full tests pass: `dotnet test --solution Acterion.Agents.AI.slnx --configuration Release`
- [ ] Release build passes: `dotnet build Acterion.Agents.AI.slnx --configuration Release`
- [ ] Dependency graph is inspected: `dotnet list src/Acterion.Agents.AI.Microsoft365.Retrieval/Acterion.Agents.AI.Microsoft365.Retrieval.csproj package --include-transitive`

**Dependencies:** Task 4
**Estimated scope:** S, 2 files

**Files likely touched:**

- `README.md`
- `specs/features/003-microsoft-identity-web-integration/implementation-evidence.md`

## Checkpoint: Feature Complete

- [ ] Every Feature 003 acceptance criterion has recorded evidence.
- [ ] Full tests and Release build pass on Microsoft Testing Platform.
- [ ] Normal verification requires no tenant, user, credential, cache, or network access.
- [ ] Human review approves Feature 003 before Feature 004 consumes it.

## Plan Approval

- [ ] Approve `AddMicrosoft365RetrievalMicrosoftIdentityWeb()` as the explicit opt-in API.
- [ ] Approve original Identity Web exception propagation.
- [ ] Approve before/after cancellation checks and the documented in-flight limitation.
- [ ] Approve preservation of an existing custom retrieval token provider.
- [ ] Approve this plan and authorize Task 1 implementation.