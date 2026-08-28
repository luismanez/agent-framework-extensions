# Feature 003: Microsoft.Identity.Web Integration

**Parent specification:** [`SPEC.md`](../../SPEC/SPEC.md)  
**Status:** Draft  
**Depends on:** Feature 001  
**Enables:** Feature 004

## Objective

Provide an explicit `Microsoft.Identity.Web` implementation of Feature 001's token-provider boundary so an authenticated ASP.NET Core web API can call Microsoft Graph through On-Behalf-Of using the current work/school user.

## User Outcome

A host already configured with Microsoft Identity Web token acquisition can opt into the integration and satisfy `IMicrosoft365RetrievalTokenProvider` without coupling the retrieval client to `HttpContext` or implementing token acquisition itself.

## Global Requirements Inherited

This feature MUST comply with the parent specification, especially:

- §12, delegated authentication and Microsoft Identity Web;
- §13.1 and §13.2, authoritative permissions and user isolation;
- §13.4, sensitive logging restrictions;
- §15, dependency-injection behavior;
- §16.2 and §17, sample-facing identity configuration;
- §18 and §19, package dependencies and stable versions;
- ADR-004 and ADR-005, delegated identity and host-owned UX.

Feature 001 owns `IMicrosoft365RetrievalTokenProvider`. If this document conflicts with the parent specification, the parent specification wins.

## Scope

### In Scope

- `MicrosoftIdentityWebRetrievalTokenProvider` backed by the stable Microsoft Identity Web token-acquisition service.
- Delegated Graph scopes required for SharePoint retrieval.
- Explicit DI opt-in that maps the Feature 001 abstraction to this implementation.
- Cancellation and safe error context.
- Unit tests using a mocked Microsoft Identity Web acquisition boundary.
- Usage documentation for hosts that already configured web API authentication and downstream token acquisition.

### Out of Scope

- Configuring host authentication, authorization policies, or endpoint protection automatically.
- App registration creation, tenant consent UX, incremental-consent UX, Conditional Access challenge handling, or browser interaction.
- Choosing or configuring token-cache persistence.
- Application permissions, managed identity for Graph, client-credentials flow, or fallback identities.
- HTTP calls to the Retrieval API and Agent Framework mapping.

## Functional Requirements

### Delegated Scopes

The implementation MUST request both delegated Microsoft Graph permissions required for SharePoint retrieval:

```text
Files.Read.All
Sites.Read.All
```

It MUST acquire a token for the current authenticated work/school user through Microsoft Identity Web. Personal Microsoft accounts and application-only tokens are unsupported.

Scope constants or configuration MUST have one authoritative definition. The host may request additional Graph scopes independently, but this integration MUST NOT silently broaden its own required scopes.

### Token Provider

`MicrosoftIdentityWebRetrievalTokenProvider` MUST implement Feature 001's exact contract:

```csharp
Task<string> GetAccessTokenAsync(
    CancellationToken cancellationToken = default);
```

The implementation MUST:

- delegate token acquisition and cache use to Microsoft Identity Web;
- pass cancellation when supported by the stable API;
- return the token only to the retrieval client;
- fail clearly when no delegated user context, consent, or usable token is available;
- preserve the underlying exception as an inner exception when wrapping adds useful package context.

It MUST NOT log, parse, persist, decorate, or expose the token in an exception.

### Dependency Injection

The integration MUST be explicitly enabled by the host. Registration MAY be a dedicated extension or an overload of `AddMicrosoft365Retrieval`, with the exact API selected during the Plan gate.

Registration MUST:

- bind `IMicrosoft365RetrievalTokenProvider` to the Microsoft Identity Web implementation;
- rely on the host's existing Microsoft Identity Web token-acquisition services;
- fail with actionable configuration context if those services are absent;
- avoid replacing unrelated token, authentication, authorization, cache, or HTTP services.

The package MUST NOT call authentication setup methods on behalf of the host.

### Failure Semantics

Authentication and consent errors belong to the host boundary. The implementation MAY wrap token-acquisition failures once to identify the failed downstream operation, but MUST leave enough type and inner-exception information for the host to apply its own claims-challenge or response behavior.

Error messages MUST NOT contain tokens, assertions, authorization headers, user claims, tenant-specific query/filter values, or Microsoft 365 content.

## Host Prerequisites

Before resolving this integration, the host is responsible for:

- authenticating the API caller with Microsoft Entra ID;
- enabling Microsoft Identity Web downstream token acquisition;
- configuring a suitable token cache;
- obtaining tenant review and consent for both delegated Graph permissions;
- protecting every endpoint that can invoke retrieval.

In-memory token caching is acceptable only for the reference sample. Production documentation MUST direct multi-instance hosts to an appropriate distributed cache strategy.

## Code Conventions

- Depend on the stable `Microsoft.Identity.Web` abstractions rather than `HttpContext`.
- Keep all token handling inside the smallest possible implementation.
- Do not catch exceptions unless adding safe, actionable context or preserving host behavior requires it.
- Do not introduce a separate authentication package in v0.1.

## Testing Strategy

Unit tests MUST cover:

- acquisition requests include both required delegated Graph scopes;
- a successful acquisition returns the exact token to the caller;
- cancellation is propagated where the Microsoft Identity Web API allows it;
- missing delegated context and representative acquisition failures remain actionable;
- wrapped failures preserve the inner exception;
- tokens and sensitive claims never appear in logs or exception messages;
- DI opt-in resolves the expected token-provider implementation and does not alter host authentication services.

Tests MUST use mocks/fakes and require no tenant, user, secret, token cache, or network access.

## Commands

```powershell
dotnet build Acterion.Agents.AI.slnx --configuration Release
dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release
dotnet test --project tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests/Acterion.Agents.AI.Microsoft365.Retrieval.Tests.csproj --configuration Release --filter-class "*MicrosoftIdentityWebRetrievalTokenProviderTests"
```

## Boundaries

### Always

- Use delegated work/school user identity and request both Graph permissions.
- Preserve host control over authentication, consent, challenges, authorization, and caching.
- Keep token values outside logs, exceptions, and telemetry.

### Ask First

- Add another authentication implementation.
- Change required scopes or registration semantics.
- Wrap Microsoft Identity Web exceptions in a new package exception type.

### Never

- Fall back to application permissions or a more privileged identity.
- Start an interactive flow or browser.
- Configure host authentication or authorization automatically.
- Cache tokens independently of Microsoft Identity Web.

## Acceptance Criteria

- [ ] An authenticated delegated-user context can supply a Graph token through Feature 001's abstraction.
- [ ] Both `Files.Read.All` and `Sites.Read.All` are requested and documented.
- [ ] Integration is explicit and does not mutate host authentication or authorization configuration.
- [ ] Missing context, consent, and token failures remain actionable without leaking sensitive data.
- [ ] Tests run without tenant credentials and the full Release build passes.

## Plan Gate Exit Evidence

- Compile a probe against centrally pinned `Microsoft.Identity.Web` that identifies the stable token-acquisition overload, cancellation support, and exception behavior.
- Select the smallest explicit DI opt-in after testing missing-service diagnostics and verifying that existing authentication registrations are unchanged.
- Record the selected API and rationale in this feature's implementation plan before adding production code.
