# Microsoft 365 Work Context Console Sample

This sample signs a user in with Microsoft Entra device code, retrieves their Microsoft 365 work context through Microsoft Graph, and adds a fresh snapshot to each Microsoft Agent Framework invocation. It follows the Retrieval console sample's device-code flow and uses Azure CLI explicitly for the Azure OpenAI model. The WorkContext package supplies the Graph client and the Agent Framework provider; this host supplies the delegated token.

## Prerequisites

- .NET 10 SDK and a Microsoft Entra work or school account.
- A public-client app registration with device-code authentication enabled. Add the delegated Microsoft Graph permissions for the facets enabled in `appsettings.json`: `User.Read` for profile, `User.Read.All` for manager, `MailboxSettings.Read` for work settings, and `Calendars.ReadBasic` for calendar. Complete consent according to your tenant policy.
- For agent mode, an Azure OpenAI chat deployment, Azure CLI installed, and an Azure CLI login with access to it. Snapshot-only mode does not require Azure OpenAI or Azure CLI.

The sample requests the explicit delegated Graph scopes for the enabled facets. It does not use application permissions. `User.Read.All` for Manager normally requires tenant admin consent.

## Configure

Copy the committed template to the ignored local settings file:

```sh
cp samples/Microsoft365WorkContext.Console/appsettings.json \
  samples/Microsoft365WorkContext.Console/appsettings.local.json
```

Set `MicrosoftEntra:TenantId` and `MicrosoftEntra:ClientId` to the tenant and public-client app registration. For agent mode, also set `AzureOpenAI:Endpoint` (HTTPS) and `AzureOpenAI:DeploymentName`. `AzureOpenAI:TenantId` is optional: leave it empty to use the tenant selected by `az login`, or set it to the tenant that should issue the model token. The `Microsoft365WorkContext` flags enable or disable each Graph facet; all four are enabled in the template. Disable facets you do not need and omit their permissions from the app registration. `MaximumCalendarEvents` defaults to 5 in this sample (valid range: 1–25) and controls both Graph `$top` and how many events the provider considers for the prompt. The prompt's overall character limit may still omit fields when the requested events are large.

The host loads `appsettings.json`, then `appsettings.local.json`, then environment variables. Environment variables use double underscores for nested keys, for example `MicrosoftEntra__TenantId` and `AzureOpenAI__Endpoint`. The local settings file is ignored by Git. No token or credential should be placed in either settings file.

Graph uses `DeviceCodeCredential`. The sample stores the selected account record under the operating system's local application-data `Acterion/Microsoft365WorkContext.Console` directory and uses an encrypted persistent token cache. Azure OpenAI uses `AzureCliCredential` directly, so Visual Studio credentials cannot be selected and no `AZURE_TOKEN_CREDENTIALS` environment variable is needed. Run `az login` before agent mode; select a specific tenant with `az login --tenant <tenant ID>` if needed.

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

Before accepting questions, agent mode checks Graph once and prints each facet's status without printing personal values. For example, `Manager: Failed (Authorization, HTTP 403)` means Graph rejected access; verify that this app registration has the delegated `User.Read.All` permission and that consent has been granted. `Manager: Unavailable` means Graph returned `404`, which normally means no manager is assigned to the signed-in account. The provider fetches a fresh snapshot again for each question, so this startup check is diagnostic, not the context sent to the model.

A `200 OK` from the Graph batch endpoint does not mean its individual operations succeeded: check each operation's status. If all enabled facets fail, the sample stops before calling the model. Check **App registrations → your app → API permissions** in the Microsoft Entra admin center. Add the Microsoft Graph **delegated** permissions for the enabled facets and grant tenant admin consent where required. If you reuse the Retrieval sample's app registration, its existing `Files.Read.All` and `Sites.Read.All` grants do not authorize WorkContext. The sample now requests the WorkContext scopes explicitly, so missing consent is surfaced during token acquisition or the startup check instead of silently continuing with Retrieval-only scopes. Do not paste access tokens into issues or chat.

## Example prompts

The values below are fictional. Responses are examples of what the agent **should** say if Graph returns the listed data; model wording is not deterministic. Try `--snapshot-only` first to see which facets and events are available for your account. The agent receives a smaller, bounded subset of that snapshot.

| Prompt to enter | Graph data needed (example) | Expected answer |
| --- | --- | --- |
| `What is my job title and department?` | `/me` has `jobTitle: "Senior Engineer"` and `department: "Platform"`. The User Profile facet must be enabled and available. | “Your job title is Senior Engineer, and your department is Platform.” If either field is missing, the agent should say it cannot see that field. |
| `Who is my manager, and where is their office?` | `/me/manager` has `displayName: "Morgan Lee"` and `officeLocation: "Madrid"`. The Manager facet must be enabled and return a manager. | “Your manager is Morgan Lee; their office location is Madrid.” A user with no manager or a failed Manager request should not receive an invented name. |
| `What working days and hours are configured for me?` | `/me/mailboxSettings/workingHours` has `daysOfWeek: ["monday", "tuesday", "wednesday", "thursday", "friday"]`, `startTime: "09:00:00"`, `endTime: "17:00:00"`, and `timeZone.name: "Romance Standard Time"`. The Work Settings facet must be enabled. | “Your configured working hours are Monday–Friday, 09:00–17:00, in the Romance Standard Time zone.” These are mailbox settings, not proof that the user is available throughout those hours. |
| `Which calendar items can you see in the next 24 hours?` | `/me/calendar/calendarView` returns a non-private event with `subject: "Project sync"`, `start`, `end`, `showAs: "busy"`, and perhaps `location.displayName: "Room 3"`. Calendar must be enabled, and the event must be in the returned page. | The agent should mention Project sync, its start and end times, and Room 3 if available. It should not claim this is a complete list of every event: the sample requests one page of at most 5 events and the service does not guarantee that page contains the nearest 5. |
| `Who organized the Project sync event?` | That calendar event also has `organizer.emailAddress.name: "Morgan Lee"`. The event must be non-private and included in the model context. | “Morgan Lee is listed as the organizer.” An organizer email address is not supplied to the agent. |
| `What can you tell me about my 14:00 calendar block?` | A returned event has `sensitivity: "private"`, `start` at 14:00 UTC, `end` at 15:00 UTC, and `showAs: "busy"`. | The agent can report a busy block from 14:00 to 15:00 UTC. It should not supply a subject, location, organizer, or attendees: those details are removed for private events. The context does not explicitly label the block as private. |
| `What does the description of Project sync say?` | Graph may have an event body, but this package does not request it or add it to the agent's context. | The agent should say it cannot see the meeting description, even if it can see the event's subject and time. |

Calendar times supplied to the agent are UTC. The provider skips empty values and values containing `@`, truncates individual values, and limits the full context to 6000 characters; a field visible in `--snapshot-only` may therefore be absent from the agent's context. If a facet is disabled, unavailable, or fails without usable values, a responsible answer should acknowledge the missing information rather than guess.

Work context is untrusted background information, never authorization evidence. The host remains responsible for authentication, consent, and access control. The automated sample tests use a fake credential and configuration; they require no tenant or Azure OpenAI resource.
