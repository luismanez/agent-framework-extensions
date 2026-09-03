# Implementation Plan: Feature 003 - Microsoft Identity Web Integration

## Overview

Implement the Microsoft Identity Web adapter for Feature 001's token-provider contract. An authenticated ASP.NET Core host remains responsible for bearer authentication, downstream token acquisition, consent, challenges, authorization, and token-cache configuration; this feature only requests the two delegated Microsoft Graph scopes and returns the resulting token to the retrieval client.

This plan is local to Feature 003. The executable checklist is [`todo.md`](todo.md), and the governing specification is [`003-microsoft-identity-web-integration.md`](003-microsoft-identity-web-integration.md).

## Planning Baseline

- Feature 001 must be implemented and approved before this plan starts.
- The centrally pinned `Microsoft.Identity.Web 4.14.2` API exposes `ITokenAcquisition.GetAccessTokenForUserAsync(IEnumerable<string>, ...)` but no overload accepts `CancellationToken`.
- Microsoft Identity Web owns OBO exchange and token-cache use; the package must not access `HttpContext` or MSAL internals.
- The Retrieval API requires both delegated `Files.Read.All` and `Sites.Read.All` permissions for SharePoint content.
- Tests use fakes and require no tenant, user, client credential, cache, token, or network connection.

## Scope Boundaries

### In Scope

- `MicrosoftIdentityWebRetrievalTokenProvider`.
- One authoritative definition of the required Graph delegated scopes.
- Explicit `IServiceCollection.AddMicrosoft365RetrievalMicrosoftIdentityWeb()` registration.
- Deterministic cancellation checks, exception preservation, safe diagnostics, XML documentation, and implementation evidence.

### Out of Scope

- Host authentication, endpoint authorization, consent/challenge UX, app registration, confidential-client credential selection, or token-cache selection.
- Application permissions, managed identity for Graph, client credentials, fallback identities, or interactive authentication.
- Retrieval HTTP calls, Agent Framework mapping, model authentication, or a separate authentication package.

## Architecture Decisions

### 1. Token-Acquisition Call

Inject `ITokenAcquisition` directly and call:

```csharp
GetAccessTokenForUserAsync(RequiredScopes)
```

`RequiredScopes` contains exactly `Files.Read.All` and `Sites.Read.All`, in that order, from one internal authoritative definition. Do not request `.default` or add scopes implicitly. Microsoft Identity Web discovers the current delegated user from the authenticated web API context and owns cache behavior.

### 2. Cancellation Semantics

Because version 4.14.2 provides no cancellation parameter, call `cancellationToken.ThrowIfCancellationRequested()` immediately before and immediately after token acquisition.

- A pre-cancelled operation never calls Identity Web.
- Cancellation while acquisition is in flight cannot abort the underlying library call.
- If cancellation is requested before acquisition completes, the acquired token is discarded and `OperationCanceledException` is returned to the caller.

This limitation must be documented and covered by a controlled incomplete-task fake. Do not use `Task.WaitAsync(cancellationToken)`: abandoning the wait would leave an unobserved shared token-acquisition operation and would not cancel Microsoft Identity Web.

### 3. Failure Semantics

Do not catch or wrap Microsoft Identity Web exceptions in v0.1. Preserving the original exception type gives hosts the best chance to recognize consent and Conditional Access claims challenges. The provider emits no logs. Empty or whitespace token output is rejected with a package-owned `InvalidOperationException` whose message names token acquisition but includes no token, claims, tenant data, or inner exception text.

### 4. Explicit DI Opt-In

Expose:

```csharp
public static IServiceCollection AddMicrosoft365RetrievalMicrosoftIdentityWeb(
    this IServiceCollection services);
```

The method uses `TryAddTransient<IMicrosoft365RetrievalTokenProvider, MicrosoftIdentityWebRetrievalTokenProvider>()`. It is idempotent, preserves a deliberately registered custom provider, and does not call any authentication, authorization, cache, or Microsoft Identity Web setup method.

Resolving the token provider without the host's `ITokenAcquisition` registration fails through standard DI with both service types visible in the diagnostic. Hosts call this method after `AddMicrosoft365Retrieval`; either registration order remains valid because service resolution is deferred.

## Dependency Graph

```text
Task 1: public contract and Identity Web API probe
    |
    +--> Task 2: token provider behavior
             |
             +--> Task 3: explicit DI opt-in
                      |
                      +--> Task 4: host guidance and security assertions
                               |
                               +--> Task 5: evidence and closure
```

## Task Plan

| Task | Outcome | Size | Depends on |
| --- | --- | --- | --- |
| 1 | Pin provider, registration, scope, and cancellation contracts | S | Feature 001 |
| 2 | Implement successful, cancelled, empty-token, and failed acquisition behavior | M | Task 1 |
| 3 | Add idempotent explicit DI integration and missing-service diagnostics | M | Task 2 |
| 4 | Document host prerequisites and prove sensitive values stay out of diagnostics | M | Task 3 |
| 5 | Record evidence and close Feature 003 | S | Task 4 |

Detailed acceptance criteria, likely files, and commands are maintained in [`todo.md`](todo.md).

## Acceptance-Criteria Coverage

| Feature criterion | Planned evidence |
| --- | --- |
| Delegated user token satisfies Feature 001 abstraction | Tasks 1 and 2 contract/provider tests |
| Both required scopes requested | Task 2 acquisition tests |
| Explicit integration does not mutate host auth | Task 3 service-descriptor tests |
| Missing context, consent, and failures remain actionable and safe | Tasks 2 and 4 failure/security tests |
| Tenant-free tests and Release build pass | Every checkpoint and Task 5 |

## Risks and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Callers assume cancellation aborts in-flight OBO | Medium | Document the 4.14.2 limitation and test checks before and after acquisition |
| Exception wrapping breaks claims-challenge handling | High | Preserve original Microsoft Identity Web exceptions |
| Custom host token provider is overwritten | High | Use `TryAddTransient` and test pre-registered provider preservation |
| Host omits Identity Web token-acquisition services | Medium | Resolve through constructor injection and assert actionable standard DI failure |
| Token or claims leak through diagnostics | High | Emit no provider logs and use allowlisted package-owned messages only |
| Permission scope expands silently | High | Keep one fixed scope definition and assert exact set and order |

## Plan Approval

Human approval is required before Task 1 implementation. Approval accepts:

1. `AddMicrosoft365RetrievalMicrosoftIdentityWeb()` as the dedicated opt-in API;
2. preserving Microsoft Identity Web exceptions rather than introducing a package exception;
3. cancellation checks before and after, with no claim that in-flight acquisition is cancellable;
4. `TryAddTransient` semantics that preserve an existing custom token provider.

## Task-Level Definition of Done

A task counts as complete only when its focused test is observed RED before implementation and GREEN afterward, its Release build succeeds, it touches no more than five files, public APIs have XML documentation, no live identity service is required, and its item in [`todo.md`](todo.md) is updated immediately.

## Authoritative Sources

- Microsoft Identity Web token acquisition API: https://learn.microsoft.com/en-us/dotnet/api/microsoft.identity.web.itokenacquisition.getaccesstokenforuserasync
- Protected web API calling a downstream API: https://learn.microsoft.com/en-us/entra/identity-platform/scenario-web-api-call-api-acquire-token
- Retrieval API permissions: https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/copilotroot-retrieval
- Restored package contract: `Microsoft.Identity.Web 4.14.2` XML documentation under the local NuGet cache
