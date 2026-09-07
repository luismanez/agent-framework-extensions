# Troubleshooting

Diagnose Microsoft 365 Retrieval one layer at a time. First prove the delegated token and Retrieval API call without a model; then add Agent Framework and the model provider.

## Fast isolation path

1. Confirm the user can open the target SharePoint content in the browser.
2. Confirm the app registration has delegated `Files.Read.All` and `Sites.Read.All` with effective consent.
3. Run the console sample in retrieval-only mode.
4. Remove optional filters and test one context-rich query.
5. Inspect the safe HTTP status and Graph request ID on `Microsoft365RetrievalException`.
6. Add the model and Agent Framework only after direct retrieval succeeds.

```sh
dotnet run --project samples/Microsoft365Retrieval.Console -- --retrieval-only
```

This mode does not require Azure OpenAI settings or `az login`. It does require the Microsoft Entra public-client configuration and a Retrieval API access path for the signed-in user.

## Capture safe diagnostics

```csharp
try
{
    IReadOnlyList<Microsoft365RetrievalHit> hits =
        await retrievalClient.RetrieveAsync(query, cancellationToken);
}
catch (Microsoft365RetrievalException exception)
{
    logger.LogWarning(
        "Retrieval failed with status {StatusCode} and request ID {RequestId}.",
        exception.StatusCode,
        exception.RequestId);
}
```

Do not add the access token, authorization header, full query, filter value, response body, or retrieved extracts to diagnostic logs.

## Status code guide

| Status | Package message | Check first |
| --- | --- | --- |
| `400` | The retrieval request was rejected. | Query length, metadata names, raw KQL, and current API contract |
| `401` | The delegated access token was rejected. | Token audience, expiry, tenant, delegated scopes, and token acquisition flow |
| `403` | The delegated user is not authorized to retrieve this content. | SharePoint access, effective consent, license or pay-as-you-go enablement, and tenant policy |
| `429` | Microsoft Graph throttled the retrieval request. | Per-user request volume, duplicate calls, concurrency, and retry policy |
| `5xx` | Microsoft Graph is temporarily unavailable. | Service health, transient retry policy, and Graph request ID |

Network failures without an HTTP response are wrapped as `Microsoft365RetrievalException` with the message `The retrieval request could not reach Microsoft Graph.` Check DNS, proxy, TLS inspection, outbound firewall policy, and cancellation.

## Authentication does not start

### Device code or public client

Check that:

- The registration belongs to the intended work or school tenant.
- **Allow public client flows** is enabled.
- The configured tenant and client IDs are not placeholders.
- The user completes the device-code flow in the same tenant.
- Tenant policy permits the flow and consent has been completed.

To switch accounts in the console sample, delete its `Acterion/Microsoft365Retrieval.Console/authentication-record.json` file from the operating system's local application-data directory. This removes account selection metadata; Azure Identity manages the encrypted token cache separately.

### On-Behalf-Of

Check that:

- The caller's token audience is the protected API.
- The caller requested the API's exposed delegated scope.
- The API registration has the downstream Graph delegated permissions.
- A valid confidential-client credential is configured.
- `EnableTokenAcquisitionToCallDownstreamApi()` and a token cache are registered.
- Token acquisition receives the authenticated `ClaimsPrincipal` for the current request.

Do not send a Graph token to the protected API as a substitute for an API-audience token, and do not fall back to client credentials when OBO fails.

## `401 Unauthorized`

A `401` from Retrieval means Microsoft Graph rejected the token supplied by the host. Verify securely that:

- Its audience is Microsoft Graph.
- It is not expired or not-yet-valid.
- It represents the expected user and tenant.
- It was acquired through a delegated flow.
- Its delegated scopes reflect the permissions configured and consented for the application.

The package requests no scopes itself. Fix the host token provider or app registration rather than changing Retrieval options.

## `403 Forbidden`

Test these independently:

1. The signed-in user can open the target document or site in SharePoint.
2. The app has effective delegated consent for `Files.Read.All` and `Sites.Read.All`.
3. The user has a Microsoft 365 Copilot license, or Retrieval API pay-as-you-go is enabled for that user and tenant.
4. Pay-as-you-go enablement has had time to propagate; Microsoft currently advises that this can take approximately two hours.
5. Tenant policy and Conditional Access requirements have been satisfied.

The first pay-as-you-go request after propagation can fail while billing policy state updates. Follow the current [pay-as-you-go documentation](https://learn.microsoft.com/microsoft-365-copilot/extensibility/api/ai-services/retrieval/paygo-retrieval) and retry only after validating configuration.

## `400 Bad Request`

The client rejects empty queries and queries longer than 1,500 characters locally. For a Graph `400`, check:

- Whether requested metadata fields are supported by the current API.
- Whether a raw filter uses supported KQL properties and syntax.
- Whether platform behavior or the preview API contract changed.

Temporarily test with the defaults and no filter. If that succeeds, reintroduce one option at a time. Keep in mind that Microsoft documents some invalid filter syntax as executing without the intended scope rather than returning an error, so a successful request does not prove a raw filter is correct.

## `429 Too Many Requests`

The current documented limit is up to 200 requests per user per hour. Look for:

- Automatic retrieval running before every model call.
- Multiple agent turns or retries for one user action.
- Duplicate requests from UI retries or concurrent server instances.
- A missing per-user rate limit.

The package does not retry. Implement capped exponential backoff with jitter at the host only when duplicate calls are acceptable for the workflow and billing model. Prefer reducing unnecessary calls before adding retries.

## Empty results

An empty result is not necessarily an error. Check:

- The user has access to relevant SharePoint content.
- The query is one specific, context-rich sentence.
- The content has been indexed by Microsoft 365.
- A trusted path or site filter points to the intended location.
- The file type and size are supported by the current Retrieval API.
- The query targets SharePoint, which is the data source this package currently sends.

Run once without `FilterExpression`. If results appear, verify the canonical SharePoint path from the item's **Details** pane rather than using a sharing link or browser address.

## Configuration validation failure

`AddMicrosoft365Retrieval` validates options when services resolve. Check that:

- `MaximumNumberOfResults` is between 1 and 25.
- `ResourceMetadata` is not null.
- Every metadata field is nonempty and not whitespace.
- The host registered an `IMicrosoft365RetrievalTokenProvider`.

For all defaults and filter constraints, see the [configuration reference](configuration.md).

## Direct retrieval works, but the agent does not use it

First identify the selected mode:

- `BeforeAIInvoke` always searches before the model call.
- `OnDemandFunctionCalling` lets the model decide whether to call the search tool.

For on-demand mode, create the agent with:

```csharp
new ChatClientAgentOptions { UseProvidedChatClientAsIs = true }
```

Without that setting, Agent Framework can apply its default decorators instead of preserving the function-invocation pipeline composed by the extension. Because the setting also disables all default agent decorators, add any other required middleware explicitly to `ChatClientBuilder`.

If on-demand retrieval is wired correctly but unused, inspect the model's tool-calling support, agent instructions, and tool selection behavior. Compare with automatic mode to separate retrieval configuration from model decision-making.

## Logging reference

Enable Information logs for the package namespace to observe:

- Event `1000`: request started, result limit, and filter presence.
- Event `1001`: completion latency and result count.
- Event `1002`: failure latency and HTTP status.
- Event `1003`: throttling latency.

These events deliberately omit sensitive request and response data. A start event without a completion or failure event can indicate host cancellation or abrupt process termination.

## Still blocked

Collect the following without including sensitive content:

- Package version and .NET SDK version.
- Host type and authentication flow.
- Retrieval mode: direct, automatic, or on demand.
- HTTP status and Graph request ID.
- Whether the same user can access the target content in SharePoint.
- Whether the issue reproduces with defaults and no filter.
- Whether the user uses a Copilot license or pay-as-you-go.

Use the Graph request ID and timestamp when engaging Microsoft support for a platform-side failure.