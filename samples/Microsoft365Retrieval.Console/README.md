# Microsoft 365 Retrieval Console Sample

This sample demonstrates automatic Microsoft 365 retrieval for an Agent Framework agent. The console host obtains a delegated Microsoft Graph token through Azure Identity device-code authentication and supplies it to the Retrieval package through a sample-local `IMicrosoft365RetrievalTokenProvider` implementation.

Set `Microsoft365Retrieval:DataSource` to `SharePoint` (the default) or `OneDriveBusiness` to select the source for this console client.

For package installation and integration choices, start with [getting started](../../docs/getting-started.md). See [Microsoft Entra ID setup](../../docs/entra-id-setup.md) for the complete public-client registration flow and [troubleshooting](../../docs/troubleshooting.md) for live-request diagnostics.

## Prerequisites

- A work or school Microsoft Entra tenant with SharePoint Online or organizational OneDrive content available to the test user.
- Retrieval API access. OneDrive requires a Microsoft 365 Copilot license for the signed-in user; [pay-as-you-go consumption does not include OneDrive](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/paygo-retrieval). SharePoint can use a Copilot license or tenant-enabled pay-as-you-go consumption.
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
		"TenantId": "<Azure OpenAI resource tenant ID>",
		"Endpoint": "https://<resource-name>.openai.azure.com/",
		"DeploymentName": "<chat-completions-deployment-name>"
	},
	"Microsoft365Retrieval": {
		"DataSource": "SharePoint",
		"SharePointSiteUrl": "https://<tenant>.sharepoint.com/sites/<site>/",
		"FilterExpression": "",
		"MaximumNumberOfResults": 8
	}
}
```

`appsettings.local.json` is excluded from Git. The sample loads the committed template first, then local settings, then environment variables. Use double underscores for hierarchical environment-variable overrides:

```text
MicrosoftEntra__TenantId=<Microsoft Entra tenant ID>
MicrosoftEntra__ClientId=<public client application ID>
AzureOpenAI__TenantId=<Azure OpenAI resource tenant ID>
AzureOpenAI__Endpoint=https://<resource-name>.openai.azure.com
AzureOpenAI__DeploymentName=<chat-completions-deployment-name>
Microsoft365Retrieval__DataSource=SharePoint
Microsoft365Retrieval__SharePointSiteUrl=https://<tenant>.sharepoint.com/sites/<site>/
Microsoft365Retrieval__MaximumNumberOfResults=8
```

The previous flat environment variables (`AZURE_TENANT_ID`, `AZURE_CLIENT_ID`, `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_DEPLOYMENT_NAME`, and `MICROSOFT365_RETRIEVAL_FILTER`) remain supported for compatibility. `AZURE_OPENAI_TENANT_ID` is also accepted for the Azure OpenAI tenant. For SharePoint, `SharePointSiteUrl` is converted to a typed `Path` filter and takes precedence over `FilterExpression`. Leave it empty to search all accessible SharePoint content. `FilterExpression` is an optional KQL filter for either source and takes precedence over the legacy `MICROSOFT365_RETRIEVAL_FILTER` variable.

To try OneDrive, keep your `MicrosoftEntra` settings and replace the retrieval section of `appsettings.local.json` with:

```json
{
	"Microsoft365Retrieval": {
		"DataSource": "OneDriveBusiness",
		"SharePointSiteUrl": "",
		"FilterExpression": "",
		"MaximumNumberOfResults": 8
	}
}
```

Run `--retrieval-only` as shown below. With an empty filter, the query can return organizational OneDrive content accessible to the signed-in user. To narrow it, set `FilterExpression` to trusted KQL such as `Path:"<canonical OneDrive path>"`. Obtain that path from the item's **Details** pane in OneDrive; a sharing link or browser address is not a reliable filter path. The sample rejects a non-empty `SharePointSiteUrl` with `OneDriveBusiness` so it cannot accidentally apply a SharePoint site filter to OneDrive. [Microsoft's Retrieval API guidance](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/api/ai-services/retrieval/overview) explains path selection.

The Retrieval package is independent of the model provider. This host uses Azure OpenAI and `DefaultAzureCredential` for the model service only; Graph retrieval always uses `DeviceCodeCredential`. `AzureOpenAI:TenantId` must identify the tenant that owns the Azure OpenAI resource. It can have the same value as `MicrosoftEntra:TenantId`, but it is configured separately because the two resources can belong to different tenants. Developer credentials that cannot authenticate in the configured tenant are skipped, so the chain can continue from Visual Studio to Azure CLI or another developer credential. At least one credential in the chain must represent an identity in that tenant with access to the configured deployment. For production, prefer a deliberately selected credential for the model service instead of `DefaultAzureCredential`.

Before running locally, authenticate Azure CLI with an identity that can invoke the configured Azure OpenAI deployment:

```sh
az login --tenant <Azure OpenAI resource tenant ID>
```

To use Azure CLI exclusively during local testing, set `AZURE_TOKEN_CREDENTIALS=AzureCliCredential` before starting the sample. This optional setting avoids probing the other credentials in the default chain.

## Run

To validate Microsoft 365 Retrieval without an Azure OpenAI resource, run:

```sh
dotnet run --project samples/Microsoft365Retrieval.Console -- --retrieval-only
```

This mode calls the Retrieval API directly and prints result titles, URLs, and retrieved extracts. It does not construct an Azure OpenAI client or invoke a model, so `AzureOpenAI` settings and `az login` are not required. Because retrieved document text is printed to the terminal, use this diagnostic mode only in an appropriate development environment.

To run the complete Agent Framework flow with automatic retrieval and an Azure OpenAI response, run:

```sh
dotnet run --project samples/Microsoft365Retrieval.Console
```

On the first run, follow the device-code instructions printed by the console. The sample stores the resulting Azure Identity `AuthenticationRecord` in the current user's local application-data directory and keeps tokens in Azure Identity's encrypted persistent cache. Later runs select the same account and authenticate silently while its refresh token remains valid.

The authentication-record file contains account metadata, not access or refresh tokens. Delete `Acterion/Microsoft365Retrieval.Console/authentication-record.json` from the operating system's local application-data directory to select another account or reset authentication. Press `Ctrl+C` to cancel a pending request or submit an empty line to exit.

`InteractiveBrowserCredential` can be used as an explicit host-local alternative where a browser is available; it is intentionally not an automatic fallback in this sample.

## Security notes

Do not commit credentials, tokens, filters containing sensitive identifiers, or local environment files. The sample prints the device-code instruction but never an access token. Retrieval-only mode deliberately prints retrieved document text for local diagnostics; the complete agent flow does not log it directly.

Treat retrieved content as untrusted model input. The agent instruction explicitly resists instructions embedded in retrieved content, but application-specific prompt-injection defenses and authorization controls remain the host's responsibility. Configure `FilterExpression` and `MICROSOFT365_RETRIEVAL_FILTER` only from trusted sources because they scope retrieval; neither is an authorization boundary.

## Verification

The sample tests use a fake `TokenCredential`; they do not require a tenant, user sign-in, Microsoft 365 data, or an Azure OpenAI service.
