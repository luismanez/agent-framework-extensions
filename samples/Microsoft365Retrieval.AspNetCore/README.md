# Microsoft 365 Retrieval ASP.NET Core Sample

This community sample demonstrates a protected ASP.NET Core API whose authenticated employee requests are grounded with permission-trimmed Microsoft 365 content. It is not an official Microsoft package.

The host validates its own bearer token, uses Microsoft Identity Web to acquire a delegated Microsoft Graph token on behalf of that caller, and supplies it through the sample-local `IMicrosoft365RetrievalTokenProvider`. The Retrieval package does not acquire identity and has no Microsoft Identity Web, MSAL, Azure Identity, or ASP.NET Core dependency.

## Prerequisites

- A work or school Microsoft Entra tenant and an account licensed for the Microsoft 365 Copilot Retrieval API.
- An Azure OpenAI Chat Completions deployment accessible to the host's Azure credential.
- A Microsoft Entra app registration for this protected API and a confidential-client credential for OBO.
- Delegated Microsoft Graph permissions `Files.Read.All` and `Sites.Read.All`, granted with the appropriate tenant consent.

Configure the app registration's exposed API scope, such as `api://<application-client-id>/access_as_user`. Clients call this API using a bearer token for that scope. The app registration must also hold the delegated Graph permissions above; the host acquires `https://graph.microsoft.com/.default` for the authenticated caller after consent.

## Configuration

`appsettings.json` contains only placeholders. Supply tenant identifiers and credentials with user secrets, environment variables, or deployment-specific secure configuration. Do not commit a client secret, certificate, access token, or real SharePoint path.

```sh
dotnet user-secrets set --project samples/Microsoft365Retrieval.AspNetCore \
  "AzureAd:TenantId" "<tenant-id>"
dotnet user-secrets set --project samples/Microsoft365Retrieval.AspNetCore \
  "AzureAd:ClientId" "<application-client-id>"
dotnet user-secrets set --project samples/Microsoft365Retrieval.AspNetCore \
  "AzureAd:ClientCredentials:0:ClientSecret" "<development-secret>"
dotnet user-secrets set --project samples/Microsoft365Retrieval.AspNetCore \
  "AzureOpenAI:Endpoint" "https://<resource-name>.openai.azure.com"
dotnet user-secrets set --project samples/Microsoft365Retrieval.AspNetCore \
  "AzureOpenAI:DeploymentName" "<chat-completions-deployment-name>"
```

The sample uses `DefaultAzureCredential` for Azure OpenAI only. For production, use a deliberately selected managed or workload identity credential for the model service. The Graph retrieval path always uses the incoming user through OBO; it has no app-only, managed-identity, or API-key fallback.

`Microsoft365Retrieval` binds the maximum result count, requested metadata, and optional `FilterExpression`. Configure a filter only from trusted application configuration. A SharePoint path filter scopes retrieval but is not authorization, and endpoint messages never become KQL.

## Run and call

Run the API at `http://localhost:5080`:

```sh
dotnet run --project samples/Microsoft365Retrieval.AspNetCore
```

Call it with a bearer token issued for the exposed API scope:

```sh
curl -X POST http://localhost:5080/api/assistant \
  -H "Authorization: Bearer <access-token>" \
  -H "Content-Type: application/json" \
  -d '{"message":"What is our remote work policy?"}'
```

The response contains only an `answer` string. When the model uses retrieval context, source-aware citations can remain embedded in that text. The sample does not expose raw documents, raw Graph responses, access tokens, exception details, or a separate citation schema.

## Retrieval modes

Automatic retrieval is the executable default:

```csharp
.UseMicrosoft365Retrieval(TextSearchProviderOptions.TextSearchBehavior.BeforeAIInvoke)
```

To enable on-demand retrieval, use the full-options overload and create the agent with `UseProvidedChatClientAsIs = true`:

```csharp
TextSearchProviderOptions retrievalOptions = new()
{
  SearchTime = TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling,
};

IChatClient onDemandClient = new ChatClientBuilder(
    openAIClient.GetChatClient(deploymentName).AsIChatClient())
  .UseMicrosoft365Retrieval(retrievalOptions)
  .Build(serviceProvider);

AIAgent onDemandAgent = onDemandClient.AsAIAgent(
  new ChatClientAgentOptions { UseProvidedChatClientAsIs = true },
  services: serviceProvider);
```

The extension composes native function invocation after `TextSearchProvider` exposes its search tool. `UseProvidedChatClientAsIs` disables Agent Framework's default agent decorators, so compose any additional decorators explicitly on `ChatClientBuilder`. In this mode, the model controls the search query; treat that query as untrusted input.

## Security considerations

Retrieval uses the authenticated caller's delegated permissions. `Files.Read.All` and `Sites.Read.All` are broad delegated permissions and require appropriate tenant review and consent. Microsoft 365 and SharePoint perform permission trimming for that current user; this sample does not bypass, emulate, or expand those ACLs.

`FilterExpression` only scopes the retrieval query. It is not an authorization boundary, and the host must not build it from an endpoint message. Application authentication, endpoint authorization, business authorization, and tool authorization remain host responsibilities.

Retrieved SharePoint content is untrusted LLM context and can contain indirect prompt injection. The prompt requests that the model resist instructions embedded in retrieved documents, but application-level guardrails may still be required. The sample treats retrieval results as context data, never as system instructions.

Do not log access tokens, authorization headers, full user queries at Information level, raw Graph responses, or retrieved document content by default. On token acquisition failures, do not retry with application credentials. This package does not implement fallback application permissions, interactive consent, or Conditional Access challenges; the host decides how to present those experiences.

The sample uses an in-memory OBO token cache for a small single-instance demonstration. Production multi-instance hosts need an appropriate distributed cache with stable user and tenant isolation, and a confidential-client credential mechanism such as a certificate, federated credential, or another secure Microsoft Identity Web-supported deployment configuration. These credentials establish the OBO client; they never provide app-only retrieval access.