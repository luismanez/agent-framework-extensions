# Feature 003: Console Reference Sample

**Parent specification:** [`SPEC.md`](../../SPEC/SPEC.md)
**Status:** Draft
**Target release:** v0.1
**Depends on:** Features 001 and 002

## 1. Purpose

Provide a small .NET console application that demonstrates Microsoft 365 Retrieval with an interactively authenticated work or school user.

The sample proves the package's identity boundary: the host acquires a delegated Microsoft Graph access token and supplies it through `IMicrosoft365RetrievalTokenProvider`; the Retrieval package does not acquire identity and does not depend on an identity SDK.

## 2. Scope

Create:

```text
samples/Microsoft365Retrieval.Console
```

The sample MUST demonstrate:

- `Azure.Identity` used by the sample host;
- device code authentication as the default console flow;
- delegated Microsoft Graph scopes `Files.Read.All` and `Sites.Read.All`;
- a sample-local implementation of `IMicrosoft365RetrievalTokenProvider` backed by `TokenCredential`;
- Agent Framework integration through `UseMicrosoft365Retrieval`;
- automatic retrieval with an optional SharePoint scope;
- configuration through environment variables or local user configuration;
- cancellation and clear authentication failures;
- no committed credentials or access tokens.

Interactive browser authentication MAY be documented as an alternative for environments where a browser is available.

## 3. Non-goals

This feature MUST NOT:

- add `Azure.Identity`, MSAL, or any other identity SDK to the base Retrieval package;
- publish a reusable Azure Identity adapter package;
- use `DefaultAzureCredential` or managed identity for the Retrieval API;
- fall back to application permissions;
- implement ASP.NET Core authentication or On-Behalf-Of;
- require a live tenant for build or automated tests.

## 4. Authentication flow

```text
Console host
    |
    | Azure.Identity device code flow
    v
Microsoft Entra ID
    |
    | delegated Graph access token
    v
Sample-local token provider
    |
    | IMicrosoft365RetrievalTokenProvider
    v
Acterion.Agents.AI.Microsoft365.Retrieval
    |
    v
Microsoft 365 Copilot Retrieval API
```

The configured app registration MUST allow public client flows and MUST have delegated Microsoft Graph permissions for:

```text
Files.Read.All
Sites.Read.All
```

The sample MUST request `https://graph.microsoft.com/.default` after the delegated permissions are configured and consented on the app registration.

## 5. Configuration contract

The sample should use environment variables for values that vary by tenant or deployment:

```text
AZURE_TENANT_ID
AZURE_CLIENT_ID
MICROSOFT365_RETRIEVAL_FILTER
```

Model-provider configuration is sample-specific and MUST remain independent from the Retrieval package.

## 6. Token provider

The sample owns an adapter equivalent to:

```csharp
internal sealed class AzureIdentityRetrievalTokenProvider(
    TokenCredential credential)
    : IMicrosoft365RetrievalTokenProvider
{
    private static readonly string[] Scopes =
        ["https://graph.microsoft.com/.default"];

    public async Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken = default)
    {
        AccessToken token = await credential.GetTokenAsync(
            new TokenRequestContext(Scopes),
            cancellationToken);

        return token.Token;
    }
}
```

The concrete implementation MAY change to match current Azure Identity APIs, but it MUST remain under the sample project.

## 7. Error handling and security

The sample MUST:

- fail clearly when tenant or client configuration is missing;
- explain device-code authentication instructions without logging the resulting token;
- avoid logging retrieved document text by default;
- preserve cancellation from the console host through token acquisition and retrieval;
- explain that retrieved content is untrusted model input.

## 8. Testing

Automated verification MUST remain tenant-independent.

Tests or build-time checks should cover:

- missing configuration;
- the sample-local token provider requesting the Graph `.default` scope;
- forwarding cancellation;
- successful construction using a fake `TokenCredential` or fake package token provider.

A live end-to-end run is optional and manual.

## 9. Acceptance criteria

- [ ] `samples/Microsoft365Retrieval.Console` builds with the solution.
- [ ] `Azure.Identity` is referenced only by the console sample or its tests.
- [ ] The sample authenticates a work or school user through device code flow.
- [ ] The sample-local adapter implements `IMicrosoft365RetrievalTokenProvider`.
- [ ] The base Retrieval package has no identity SDK dependency.
- [ ] The sample demonstrates Agent Framework automatic retrieval.
- [ ] Required delegated Graph permissions and app registration steps are documented.
- [ ] Automated verification does not require tenant credentials.
- [ ] No application-permission fallback or token logging exists.
