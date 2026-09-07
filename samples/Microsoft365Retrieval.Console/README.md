# Microsoft 365 Retrieval Console Sample

This sample demonstrates automatic Microsoft 365 retrieval for an Agent Framework agent. The console host obtains a delegated Microsoft Graph token through Azure Identity device-code authentication and supplies it to the Retrieval package through a sample-local `IMicrosoft365RetrievalTokenProvider` implementation.

For package installation and integration choices, start with [getting started](../../docs/getting-started.md). See [Microsoft Entra ID setup](../../docs/entra-id-setup.md) for the complete public-client registration flow and [troubleshooting](../../docs/troubleshooting.md) for live-request diagnostics.

## Prerequisites

- A work or school Microsoft Entra tenant with SharePoint Online content available to the test user.
- Retrieval API access through a Microsoft 365 Copilot license for that user or tenant-enabled pay-as-you-go consumption.
- A public-client app registration configured as described below.
- .NET 10 SDK.

Azure OpenAI is required only for the complete agent flow, not for `--retrieval-only`.

## App registration

Create a Microsoft Entra app registration for a work or school tenant, then enable public client flows. Add these delegated Microsoft Graph permissions and complete consent according to tenant policy:

```text
Files.Read.All
Sites.Read.All
```

The sample requests `https://graph.microsoft.com/.default`, which uses the permissions already configured and consented for the app. It does not use application permissions or an app-only fallback.

## Configuration

Copy the committed template to the ignored local settings file:

```sh
cp samples/Microsoft365Retrieval.Console/appsettings.json \
	samples/Microsoft365Retrieval.Console/appsettings.local.json
```

Configure the local file with your tenant-specific values:

```json
{
	"MicrosoftEntra": {
		"TenantId": "<Microsoft Entra tenant ID>",
		"ClientId": "<public client application ID>"
	},
	"AzureOpenAI": {
		"Endpoint": "https://<resource-name>.openai.azure.com/",
		"DeploymentName": "<chat-completions-deployment-name>"
	},
	"Microsoft365Retrieval": {
		"SharePointSiteUrl": "https://<tenant>.sharepoint.com/sites/<site>/",
		"MaximumNumberOfResults": 8
	}
}
```

`appsettings.local.json` is excluded from Git. The sample loads the committed template first, then local settings, then environment variables. Use double underscores for hierarchical environment-variable overrides:

```text
MicrosoftEntra__TenantId=<Microsoft Entra tenant ID>
MicrosoftEntra__ClientId=<public client application ID>
AzureOpenAI__Endpoint=https://<resource-name>.openai.azure.com
AzureOpenAI__DeploymentName=<chat-completions-deployment-name>
Microsoft365Retrieval__SharePointSiteUrl=https://<tenant>.sharepoint.com/sites/<site>/
Microsoft365Retrieval__MaximumNumberOfResults=8
```

The previous flat environment variables (`AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_DEPLOYMENT_NAME`, and `MICROSOFT365_RETRIEVAL_FILTER`) remain supported for compatibility. `SharePointSiteUrl` is converted to a typed `Path` filter. Leave it empty to search all SharePoint content available to the signed-in user, or use the legacy raw-filter variable only for advanced KQL scenarios.

The Retrieval package is independent of the model provider. This host uses Azure OpenAI and `DefaultAzureCredential` for the model service only; Graph retrieval always uses `DeviceCodeCredential`. For production, prefer a deliberately selected credential for the model service instead of `DefaultAzureCredential`.

Before running locally, authenticate Azure CLI with an identity that can invoke the configured Azure OpenAI deployment:

```sh
az login
```

## Run

To validate Microsoft 365 Retrieval without an Azure OpenAI resource, run:

```sh
dotnet run --project samples/Microsoft365Retrieval.Console -- --retrieval-only
```

This mode calls the Retrieval API directly and prints result titles, SharePoint URLs, and retrieved extracts. It does not construct an Azure OpenAI client or invoke a model, so `AzureOpenAI` settings and `az login` are not required. Because retrieved document text is printed to the terminal, use this diagnostic mode only in an appropriate development environment.

To run the complete Agent Framework flow with automatic retrieval and an Azure OpenAI response, run:

```sh
dotnet run --project samples/Microsoft365Retrieval.Console
```

On the first run, follow the device-code instructions printed by the console. The sample stores the resulting Azure Identity `AuthenticationRecord` in the current user's local application-data directory and keeps tokens in Azure Identity's encrypted persistent cache. Later runs select the same account and authenticate silently while its refresh token remains valid.

The authentication-record file contains account metadata, not access or refresh tokens. Delete `Acterion/Microsoft365Retrieval.Console/authentication-record.json` from the operating system's local application-data directory to select another account or reset authentication. Press `Ctrl+C` to cancel a pending request or submit an empty line to exit.

`InteractiveBrowserCredential` can be used as an explicit host-local alternative where a browser is available; it is intentionally not an automatic fallback in this sample.

## Security notes

Do not commit credentials, tokens, filters containing sensitive identifiers, or local environment files. The sample prints the device-code instruction but never an access token. Retrieval-only mode deliberately prints retrieved document text for local diagnostics; the complete agent flow does not log it directly.

Treat retrieved content as untrusted model input. The agent instruction explicitly resists instructions embedded in retrieved content, but application-specific prompt-injection defenses and authorization controls remain the host's responsibility. Configure `MICROSOFT365_RETRIEVAL_FILTER` only from a trusted source because it scopes which SharePoint content can be retrieved.

## Verification

The sample tests use a fake `TokenCredential`; they do not require a tenant, user sign-in, Microsoft 365 data, or an Azure OpenAI service.