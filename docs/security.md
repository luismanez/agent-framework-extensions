# Security and production guidance

Microsoft 365 Retrieval crosses three independent trust boundaries: the host application authenticates a user, the package calls Microsoft Graph with that delegated identity, and retrieved document text may be passed to a model. Preserve each boundary explicitly.

## Security model

```text
Authenticated user
    -> host authentication and authorization
    -> delegated Microsoft Graph token
    -> Microsoft 365 Retrieval API
    -> permission-trimmed SharePoint extracts
    -> host policy and model guardrails
```

Microsoft 365 and SharePoint decide which content the delegated user can access. The package does not expand, emulate, or replace those permissions. The host still decides whether that user may access an endpoint, perform a business operation, invoke retrieval, or send the resulting content to a model.

## Primary threats and controls

| Threat | Control |
| --- | --- |
| A token for the wrong user, tenant, or audience | Acquire Graph tokens per operation through the host's established delegated flow |
| Delegated acquisition fails and code escalates to app-only | Fail closed; never retry with application credentials |
| A user or model changes the KQL scope | Build filters only from trusted application configuration |
| A filter is missing, malformed, or ignored | Enforce authorization independently; treat filters only as query scope |
| Retrieved text contains indirect prompt injection | Treat extracts as untrusted data, not instructions |
| Logs disclose content or credentials | Log operational metadata, not tokens, queries, documents, or raw responses |
| One user's cached token or result reaches another user | Partition caches and state by stable user and tenant identity |
| Repeated retrieval causes throttling or cost growth | Apply host-level rate limits, cancellation, bounded retries, and cost monitoring |

## Delegated identity only

`IMicrosoft365RetrievalTokenProvider` must return a delegated Microsoft Graph token for the current user. Application permissions, client-credential Graph tokens, and managed-identity Graph tokens are outside the package contract.

Production requirements:

- Validate incoming tokens before initiating OBO.
- Preserve user and tenant identity across asynchronous work.
- Keep confidential-client credentials separate from Graph access tokens.
- Use a token cache designed for the host topology and partitioned by user and tenant.
- Fail the operation when delegated acquisition, consent, or Conditional Access fails.
- Do not substitute a service identity to make the request succeed.

For registration and flow details, see [Microsoft Entra ID setup](entra-id-setup.md).

## Authorization and retrieval scope

Authentication answers who the caller is. SharePoint permission trimming answers which documents that identity may retrieve. Your application must separately answer whether the caller may use a feature or perform an operation.

Apply authorization before invoking retrieval:

```csharp
app.MapPost("/api/assistant", HandleAssistantRequest)
    .RequireAuthorization("CanUseKnowledgeAssistant");
```

Do not use `FilterExpression` as the policy check. Microsoft documents that a Retrieval API query with incorrect KQL can execute without the intended scoping. A filter may improve relevance or constrain an approved corpus, but it is not a fail-closed security boundary.

Use the typed `SharePointRetrievalFilter` for trusted SharePoint properties and logical composition. Its text factories reject KQL structural characters rather than accepting arbitrary expressions. If advanced raw KQL is required:

- Store it in controlled configuration.
- Validate and test it before deployment.
- Prevent endpoint input and model output from reaching it.
- Monitor whether a filter is configured without recording its potentially sensitive value.

## Treat retrieved content as untrusted

SharePoint documents can contain malicious, obsolete, or irrelevant instructions. A model can mistake those instructions for application intent.

Recommended controls:

- Delimit retrieved extracts as reference data in prompts.
- Instruct the model to follow system and developer policy over document instructions.
- Do not grant tools solely because retrieved text requests an action.
- Authorize every consequential tool call independently of model reasoning.
- Require confirmation or deterministic validation for high-impact actions.
- Return source links where appropriate so users can inspect supporting material.
- Evaluate prompt-injection and data-exfiltration cases using representative tenant content.

On-demand mode adds another untrusted value: the model-generated search query. It is safe to pass as the natural-language `queryString`, but it must never be reused as raw KQL or an authorization decision.

## Data handling and privacy

Retrieval hits can contain confidential organizational text and metadata. Define where that data may flow before enabling the feature.

- Confirm that the selected model service and deployment meet the organization's data-handling requirements.
- Minimize requested metadata and the number of results sent downstream.
- Avoid persisting raw extracts unless the application has a documented retention purpose.
- Apply the same classification, retention, deletion, and incident-response controls used for the source content.
- Do not expose raw hits or extracts to clients unless the product explicitly requires it and the response is authorized.
- Review telemetry processors, exception reporters, and request capture before production.

Direct retrieval keeps model infrastructure optional. If a use case only needs search results, do not send document text to a model.

## Secrets and credentials

Do not commit or log:

- Client secrets or certificate private keys.
- Access or refresh tokens.
- Authorization headers.
- Populated local settings files.
- Authentication cache files.

Use user secrets only for local development. Prefer certificates, federated credentials, managed deployment integrations, or another approved confidential-client mechanism for hosted applications. Rotate credentials and restrict access to their backing stores.

Public clients must not use client secrets. Device-code and interactive flows rely on user interaction and a public client registration.

## Logging and diagnostics

The package emits structured operational events without queries, tokens, filters, URLs, metadata, or document content:

| Event ID | Name | Level | Data |
| --- | --- | --- | --- |
| `1000` | `RetrievalStarted` | Information | Maximum result count and whether a filter is configured |
| `1001` | `RetrievalCompleted` | Information | Elapsed milliseconds and result count |
| `1002` | `RetrievalFailed` | Warning | Elapsed milliseconds and HTTP status, when available |
| `1003` | `RetrievalThrottled` | Warning | Elapsed milliseconds |

`Microsoft365RetrievalException` exposes a safe message, optional HTTP status, and optional Microsoft Graph request ID. Record the status and request ID for support correlation, subject to your telemetry policy. Do not add the token, query, raw response body, or retrieved text to the same event.

## Availability, throttling, and cost

Microsoft's current platform limits include up to 200 Retrieval API requests per user per hour. Design the host to:

- Propagate cancellation tokens.
- Bound concurrent requests per user.
- Avoid duplicate retrieval in the same workflow.
- Retry only transient failures with capped exponential backoff and jitter.
- Never automatically retry authentication or authorization failures.
- Monitor throttling, latency, result counts, and pay-as-you-go meter usage.

The package does not implement retries. This avoids hidden duplicate calls and lets the host align retry behavior with its latency and billing requirements.

## Production checklist

- [ ] Delegated Graph permissions are reviewed and consented according to tenant policy.
- [ ] No application-permission fallback exists.
- [ ] Endpoint, business, and tool authorization are enforced outside retrieval filters.
- [ ] Token and result caches are isolated by user and tenant.
- [ ] Production confidential credentials do not rely on committed secrets.
- [ ] Queries, document text, tokens, and raw Graph responses are excluded from routine logs.
- [ ] Retrieved content and model-generated search queries are treated as untrusted.
- [ ] Rate limits, cancellation, retries, and pay-as-you-go costs are monitored.
- [ ] Error responses to clients are generic and do not expose downstream details.
- [ ] Prompt-injection, cross-user isolation, and authorization failure cases are tested.

Review the current [Retrieval API overview and limitations](https://learn.microsoft.com/microsoft-365-copilot/extensibility/api/ai-services/retrieval/overview) before release because preview behavior and limits can change.
