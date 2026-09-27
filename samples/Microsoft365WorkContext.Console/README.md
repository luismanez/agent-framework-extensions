# Microsoft 365 Work Context Console Sample

This sample signs a user in with Microsoft Entra device code, retrieves their Microsoft 365 work context through Microsoft Graph, and adds a fresh snapshot to each Microsoft Agent Framework invocation. It follows the Retrieval console sample's authentication and Azure OpenAI setup. The WorkContext package supplies the Graph client and the Agent Framework provider; this host supplies the delegated token.

## Prerequisites

- .NET 10 SDK and a Microsoft Entra work or school account.
- A public-client app registration with device-code authentication enabled. Add the delegated Microsoft Graph permissions for the facets enabled in `appsettings.json`: `User.Read` for profile, `User.Read.All` for manager, `MailboxSettings.Read` for work settings, and `Calendars.ReadBasic` for calendar. Complete consent according to your tenant policy.
- For agent mode, an Azure OpenAI chat deployment and an identity with access to it. Snapshot-only mode does not require Azure OpenAI.

The sample requests `https://graph.microsoft.com/.default`, using the Graph permissions configured and consented for the app. It does not use application permissions.

## Configure

Copy the committed template to the ignored local settings file:

```sh
cp samples/Microsoft365WorkContext.Console/appsettings.json \
  samples/Microsoft365WorkContext.Console/appsettings.local.json
```

Set `MicrosoftEntra:TenantId` and `MicrosoftEntra:ClientId` to the tenant and public-client app registration. For agent mode, also set `AzureOpenAI:TenantId`, `AzureOpenAI:Endpoint` (HTTPS), and `AzureOpenAI:DeploymentName`. The two tenant IDs may differ. The `Microsoft365WorkContext` flags enable or disable each Graph facet; all four are enabled in the template. Disable facets you do not need and omit their permissions from the app registration. `MaximumCalendarEvents` defaults to 5 in this sample (valid range: 1–25) and controls both Graph `$top` and how many events the provider considers for the prompt. The prompt's overall character limit may still omit fields when the requested events are large.

The host loads `appsettings.json`, then `appsettings.local.json`, then environment variables. Environment variables use double underscores for nested keys, for example `MicrosoftEntra__TenantId` and `AzureOpenAI__Endpoint`. The local settings file is ignored by Git. No token or credential should be placed in either settings file.

Graph uses `DeviceCodeCredential`. The sample stores the selected account record under the operating system's local application-data `Acterion/Microsoft365WorkContext.Console` directory and uses an encrypted persistent token cache. Azure OpenAI uses `DefaultAzureCredential` with the configured model tenant, as in the Retrieval console sample. For local Azure CLI credentials, run `az login --tenant <Azure OpenAI tenant ID>` before agent mode.

## Run

To inspect one fresh Graph snapshot without a model:

```sh
dotnet run --project samples/Microsoft365WorkContext.Console -- --snapshot-only
```

This prints the snapshot as JSON, including available profile, manager, work settings, and calendar metadata. Run it only in an appropriate development terminal because it displays personal work data. The package suppresses private calendar descriptions and omits addresses from its public snapshot.

To ask an agent questions using WorkContext:

```sh
dotnet run --project samples/Microsoft365WorkContext.Console
```

The first run displays a device-code sign-in instruction. The agent fetches new work context for each question through `UseMicrosoft365WorkContext()`. Enter an empty line to exit or press `Ctrl+C` to cancel. The default `BestEffort` mode allows an available facet to contribute even if another fails; snapshot-only output includes each facet's status and sanitized failure metadata.

Work context is untrusted background information, never authorization evidence. The host remains responsible for authentication, consent, and access control. The automated sample tests use a fake credential and configuration; they require no tenant or Azure OpenAI resource.
