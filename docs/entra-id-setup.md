# Microsoft Entra ID setup

Microsoft 365 Retrieval is a delegated operation: every request runs for a signed-in work or school user. The package accepts the resulting Microsoft Graph token through `IMicrosoft365RetrievalTokenProvider` and does not perform authentication itself.

This guide covers two common registrations:

- A public client for a console or desktop application.
- A protected web API that calls Microsoft Graph on behalf of its caller.

## Required Microsoft Graph permissions

Add these **delegated** Microsoft Graph permissions to the app registration:

| Permission | Purpose |
| --- | --- |
| `Files.Read.All` | Read files the signed-in user can access |
| `Sites.Read.All` | Read documents and list items on behalf of the signed-in user |

Do not add the application variants for this package. There is no app-only fallback.

Microsoft's permission catalog marks the delegated variants as not requiring administrator consent by default. However, tenant consent policies, permission classifications, Conditional Access, or disabled user consent can still require administrator approval. Treat both as broad delegated permissions and obtain the review appropriate for your organization.

The samples request the `https://graph.microsoft.com/.default` scope. This asks Microsoft Entra ID to issue a token containing the Graph delegated permissions already configured and consented for the application; it does not grant permissions by itself.

## Public client registration

Use this pattern for console, desktop, and other installed applications that cannot safely keep a client secret.

### Create and configure the registration

1. In the Microsoft Entra admin center, open **Identity > Applications > App registrations**.
2. Create a registration for accounts in the intended organizational directory. The reference sample is single-tenant.
3. Record the **Application (client) ID** and **Directory (tenant) ID**.
4. Under **Authentication > Advanced settings**, set **Allow public client flows** to **Yes**.
5. Under **API permissions**, add the delegated Microsoft Graph permissions `Files.Read.All` and `Sites.Read.All`.
6. Complete user or administrator consent according to tenant policy.

Do not create a client secret for a public client. Device code and interactive browser flows use the client ID and user interaction, not a confidential credential.

### Configure the console sample

```json
{
  "MicrosoftEntra": {
    "TenantId": "<tenant-id>",
    "ClientId": "<public-client-application-id>"
  }
}
```

The sample uses `DeviceCodeCredential`, requests `https://graph.microsoft.com/.default`, and persists both its Azure Identity authentication record and encrypted token cache. See the [console sample guide](../samples/Microsoft365Retrieval.Console/README.md) for configuration and reset instructions.

## Protected web API registration

Use this pattern when an authenticated client calls your ASP.NET Core API and the API must retrieve SharePoint content for that same user. The API validates its own bearer token and exchanges it for a delegated Microsoft Graph token through OAuth 2.0 On-Behalf-Of (OBO).

### Create and configure the registration

1. Create an app registration for the protected API and record its tenant and client IDs.
2. Under **Expose an API**, set an Application ID URI such as `api://<application-client-id>`.
3. Add a delegated scope such as `access_as_user` and decide who can consent to it.
4. Configure the calling client application to request that exposed API scope.
5. Under the API registration's **API permissions**, add delegated Microsoft Graph `Files.Read.All` and `Sites.Read.All`.
6. Complete consent for both the client-to-API scope and the API's downstream Graph permissions.
7. Configure a confidential-client credential for the API.

The incoming access token must target your API, not Microsoft Graph. Microsoft Identity Web then performs OBO and obtains a separate Graph token for the caller.

### Configure Microsoft Identity Web

```csharp
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
    .EnableTokenAcquisitionToCallDownstreamApi()
    .AddInMemoryTokenCaches();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<
    IMicrosoft365RetrievalTokenProvider,
    MicrosoftIdentityWebRetrievalTokenProvider>();
```

The token provider requests Graph for the current `HttpContext.User`:

```csharp
private static readonly string[] GraphScopes =
    ["https://graph.microsoft.com/.default"];

public Task<string> GetAccessTokenAsync(
    CancellationToken cancellationToken = default)
{
    ClaimsPrincipal user = httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("A delegated user is required.");

    return tokenAcquisition.GetAccessTokenForUserAsync(
        GraphScopes,
        user: user);
}
```

The complete implementation in the [ASP.NET Core sample](../samples/Microsoft365Retrieval.AspNetCore/Authentication/MicrosoftIdentityWebRetrievalTokenProvider.cs) resolves `ITokenAcquisition` from the request services and propagates cancellation.

### Choose a confidential credential

A protected API needs a credential to prove its own identity during OBO. That credential establishes the confidential client; it does not change Retrieval into an app-only operation.

- Use a client secret only for local development and store it in user secrets or another secure local store.
- Prefer a certificate, federated credential, managed deployment integration, or another Microsoft Identity Web-supported credential mechanism in production.
- Never commit a secret, certificate private key, token, or populated environment file.

The reference sample uses an in-memory token cache for a small single-instance application. A multi-instance deployment needs a distributed cache with stable user and tenant isolation.

## Consent and Conditional Access

The package does not implement interactive consent, incremental consent, or Conditional Access claims-challenge handling. The host must decide how to present and recover from those experiences.

For a web API:

- Return an appropriate challenge to the client instead of attempting interactive sign-in on the server.
- Preserve the caller's identity throughout token acquisition.
- Never retry a failed delegated acquisition with application credentials.
- Avoid exposing token acquisition details or downstream exception bodies to callers.

## Token validation checklist

When a request returns `401` or `403`, inspect a test token securely and verify:

- The token audience is Microsoft Graph when it reaches the package.
- The token represents the expected user and tenant.
- Delegated scope claims include the required consented permissions.
- The user can open the target SharePoint content directly.
- The user has Retrieval API access through licensing or pay-as-you-go.
- Conditional Access requirements have been satisfied by the host.

Do not paste production tokens into public token-inspection tools or logs.

## Official references

- [Microsoft Graph permissions reference](https://learn.microsoft.com/graph/permissions-reference)
- [OAuth 2.0 On-Behalf-Of flow](https://learn.microsoft.com/entra/identity-platform/v2-oauth2-on-behalf-of-flow)
- [Microsoft Identity Web web API guidance](https://learn.microsoft.com/entra/identity-platform/scenario-web-api-call-api-overview)
- [Azure Identity device code authentication](https://learn.microsoft.com/dotnet/api/azure.identity.devicecodecredential)
