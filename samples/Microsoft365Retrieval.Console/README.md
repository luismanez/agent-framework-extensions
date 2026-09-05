# Microsoft 365 Retrieval Console Sample

This sample demonstrates automatic Microsoft 365 retrieval for an Agent Framework agent. The console host obtains a delegated Microsoft Graph token through Azure Identity device-code authentication and supplies it to the Retrieval package through a sample-local `IMicrosoft365RetrievalTokenProvider` implementation.

## App registration

Create a Microsoft Entra app registration for a work or school tenant, then enable public client flows. Add and grant consent for these delegated Microsoft Graph permissions:

```text
Files.Read.All
Sites.Read.All
```

The sample requests `https://graph.microsoft.com/.default`, which uses the permissions already configured and consented for the app. It does not use application permissions or an app-only fallback.

## Configuration

Set these environment variables before running the sample:

```text
AZURE_TENANT_ID=<Microsoft Entra tenant ID>
AZURE_CLIENT_ID=<public client application ID>
AZURE_OPENAI_ENDPOINT=https://<resource-name>.openai.azure.com
AZURE_OPENAI_DEPLOYMENT_NAME=<chat-completions-deployment-name>
MICROSOFT365_RETRIEVAL_FILTER=<optional trusted filter expression>
```

The Retrieval package is independent of the model provider. This host uses Azure OpenAI and `DefaultAzureCredential` for the model service only; Graph retrieval always uses `DeviceCodeCredential`. For production, prefer a deliberately selected credential for the model service instead of `DefaultAzureCredential`.

## Run

```sh
dotnet run --project samples/Microsoft365Retrieval.Console
```

On the first token request, follow the device-code instructions printed by the console. Authentication does not begin until a non-empty question requires retrieval. Press `Ctrl+C` to cancel a pending request or submit an empty line to exit.

`InteractiveBrowserCredential` can be used as an explicit host-local alternative where a browser is available; it is intentionally not an automatic fallback in this sample.

## Security notes

Do not commit credentials, tokens, filters containing sensitive identifiers, or local environment files. The sample prints the device-code instruction but never an access token, and it does not log retrieved document text.

Treat retrieved content as untrusted model input. The agent instruction explicitly resists instructions embedded in retrieved content, but application-specific prompt-injection defenses and authorization controls remain the host's responsibility. Configure `MICROSOFT365_RETRIEVAL_FILTER` only from a trusted source because it scopes which SharePoint content can be retrieved.

## Verification

The sample tests use a fake `TokenCredential`; they do not require a tenant, user sign-in, Microsoft 365 data, or an Azure OpenAI service.