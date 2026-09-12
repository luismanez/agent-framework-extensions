# Spec: Microsoft 365 Work Context Core

**Initiative:** [`capability-map.md`](capability-map.md)
**Parent repository specification:** [`SPEC.md`](../../SPEC/SPEC.md)
**Module id:** `work-context-core`
**Origin:** [`microsoft365-work-context-provider.md`](../../../docs/ideas/microsoft365-work-context-provider.md)
**Status:** Approved; planning not started
**Depends on:** Repository foundation
**Enables:** `agent-framework-provider`, `console-sample`, and `aspnetcore-obo-sample`

## Objective

Create the host-independent core of `Acterion.Agents.AI.Microsoft365.WorkContext`: a small .NET client that retrieves a fresh, bounded snapshot of the signed-in user's work context from Microsoft Graph using delegated identity.

The client serves .NET developers building enterprise agents. A developer can enable any combination of User Profile, Manager, Work Settings, and Calendar facets, request one snapshot, and receive explicit per-facet outcomes without constructing Graph requests or interpreting JSON batch responses.

Success means:

- Profile-only configuration is useful with no opt-in facets.
- Enabling several facets normally produces one outbound Graph HTTP request.
- One failed facet does not discard successful facets in the default mode.
- The snapshot never contains tokens, claims, contact addresses, calendar bodies, attachments, or arbitrary Graph fields.
- The package remains stateless, hosting-agnostic, and independent of identity SDKs.

This module does not inject data into a model. The dependent `agent-framework-provider` module owns model-context rendering and `AIContextProvider` integration.

## Assumptions Confirmed for This Spec

1. The package is `Acterion.Agents.AI.Microsoft365.WorkContext` with root namespace `Acterion.Agents.AI.Microsoft365.WorkContext`.
2. V1 contains all four facets. User Profile is enabled by default; Manager, Work Settings, and Calendar require explicit opt-in.
3. The public snapshot is independently consumable outside Agent Framework.
4. Microsoft Graph is called through `HttpClient` and `System.Text.Json`; the package does not depend on the Microsoft Graph SDK.
5. Best effort is the default; a global fail-fast mode is available.
6. Calendar defaults to the next 24 hours with at most 10 returned events.
7. Cancelled events and events declined by the signed-in user are omitted. All-day and free events remain with explicit indicators.
8. Private events preserve time and availability but suppress subject, location, organizer, and attendee names.
9. Attendee addresses are not exposed downstream. Graph returns attendee name and address together, so addresses can be present transiently in the HTTP payload even though response DTOs, snapshots, logs, exceptions, and model context do not retain them.
10. A missing manager or unavailable mailbox/calendar is not itself a fail-fast error. Cancellation and invalid local configuration always propagate.

## Global Requirements Inherited

This module inherits the parent specification's repository-wide requirements for package naming, delegated identity, host-owned token acquisition, authorization boundaries, minimal dependencies, .NET 10, safe diagnostics, tests, documentation, packaging, and implementation discipline. The Retrieval package's endpoint-specific API and release criteria do not apply to this independent package.

If this module conflicts with a repository-wide requirement in the parent specification, the parent specification wins until it is explicitly amended.

## Scope

### In Scope

- A new packable .NET project and test project for `Acterion.Agents.AI.Microsoft365.WorkContext`.
- Public options for facet enablement, error behavior, calendar look-ahead, and maximum event count.
- A host-provided delegated-token abstraction.
- A public asynchronous client returning an immutable `WorkContextSnapshot`.
- Explicit status and sanitized failure information for every facet.
- Direct Microsoft Graph v1.0 requests and JSON batching.
- Minimal response DTOs and explicit mapping to public snapshot models.
- Partial batch failure, outer request failure, malformed response, and cancellation behavior.
- Dependency injection registration, option validation, safe logging, and deterministic time through `TimeProvider` when supplied by the host.
- Tenant-independent tests using fake token providers, HTTP handlers, loggers, and time providers.
- Documentation of delegated permissions, least-data behavior, and unresolved Graph permission questions.

### Out of Scope

- Agent Framework `AIContextProvider`, prompt formatting, chat-client middleware, or model-context injection.
- Azure Identity, MSAL, Microsoft Identity Web, ASP.NET Core, Azure Functions, or host-specific authentication adapters.
- Token acquisition, token caching, login, consent UI, claims challenges, or credential selection.
- Application-only credentials or fallback identity.
- Graph-data caching, persistence, distributed state, conversation memory, or session state.
- Business authorization, endpoint authorization, tool authorization, or policy evaluation.
- Arbitrary Graph fields, custom facets, plug-ins, extension properties, people search, organizational chains, or calendars other than the signed-in user's default calendar.
- Calendar bodies, body previews, attachments, online-meeting URLs, event links, attendee addresses, user email addresses, phone numbers, user principal names, or Graph object identifiers.
- Automatic retries, hedging, circuit breakers, or custom resilience frameworks.
- Paging beyond the configured first calendar page.

## Tech Stack

- .NET SDK `10.0.300`, pinned by `global.json`.
- Target framework `net10.0` with nullable references, implicit usings, and deterministic builds from repository build props.
- `System.Net.Http`, `System.Text.Json`, and `TimeProvider` from the BCL.
- Microsoft.Extensions dependency injection, HTTP, logging, and options packages at the repository's centrally managed `10.0.11` versions.
- xUnit v3 `4.0.0` on Microsoft Testing Platform for tests.
- Microsoft Graph REST v1.0 only. No beta endpoints and no Microsoft Graph SDK.

The `Microsoft.Agents.AI` dependency belongs to the dependent `agent-framework-provider` module and MUST NOT be required to implement or consume the core client contract.

## Public API Contract

The module MUST expose the following shape. Public result types MUST be sealed, read-only, and privately or internally constructed. XML documentation is required for every public type and member.

```csharp
public enum WorkContextErrorBehavior
{
    BestEffort,
    FailFast,
}

public enum WorkContextFacet
{
    UserProfile,
    Manager,
    WorkSettings,
    Calendar,
}

public enum WorkContextFacetStatus
{
    Disabled,
    Available,
    Unavailable,
    Failed,
}

public enum WorkContextFailureKind
{
    TokenAcquisition,
    Transport,
    Authentication,
    Authorization,
    Throttled,
    Service,
    InvalidResponse,
}

public sealed class Microsoft365WorkContextOptions
{
    public bool EnableUserProfile { get; set; } = true;
    public bool EnableManager { get; set; }
    public bool EnableWorkSettings { get; set; }
    public bool EnableCalendar { get; set; }
    public WorkContextErrorBehavior ErrorBehavior { get; set; }
        = WorkContextErrorBehavior.BestEffort;
    public TimeSpan CalendarLookAhead { get; set; }
        = TimeSpan.FromHours(24);
    public int MaximumCalendarEvents { get; set; } = 10;
}

public interface IMicrosoft365WorkContextTokenProvider
{
    Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken = default);
}

public interface IMicrosoft365WorkContextClient
{
    Task<WorkContextSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default);
}

public sealed class WorkContextSnapshot
{
    public DateTimeOffset CapturedAtUtc { get; }
    public WorkContextFacetResult<WorkContextUserProfile> UserProfile { get; }
    public WorkContextFacetResult<WorkContextManager> Manager { get; }
    public WorkContextFacetResult<WorkContextWorkSettings> WorkSettings { get; }
    public WorkContextFacetResult<IReadOnlyList<WorkContextCalendarEvent>> Calendar { get; }
}

public sealed class WorkContextFacetResult<T> where T : class
{
    public WorkContextFacetStatus Status { get; }
    public T? Value { get; }
    public WorkContextFacetFailure? Failure { get; }
}

public sealed class WorkContextFacetFailure
{
    public WorkContextFailureKind Kind { get; }
    public System.Net.HttpStatusCode? StatusCode { get; }
    public string? RequestId { get; }
}

public sealed class WorkContextUserProfile
{
    public string? DisplayName { get; }
    public string? GivenName { get; }
    public string? Surname { get; }
    public string? JobTitle { get; }
    public string? Department { get; }
    public string? OfficeLocation { get; }
    public string? PreferredLanguage { get; }
}

public sealed class WorkContextManager
{
    public string? DisplayName { get; }
    public string? JobTitle { get; }
    public string? Department { get; }
    public string? OfficeLocation { get; }
}

public sealed class WorkContextWorkSettings
{
    public string? TimeZone { get; }
    public WorkContextLocale? Language { get; }
    public WorkContextWorkingHours? WorkingHours { get; }
}

public sealed class WorkContextLocale
{
    public string? Locale { get; }
    public string? DisplayName { get; }
}

public sealed class WorkContextWorkingHours
{
    public IReadOnlyList<DayOfWeek> DaysOfWeek { get; }
    public TimeOnly StartTime { get; }
    public TimeOnly EndTime { get; }
    public string? TimeZone { get; }
}

public sealed class WorkContextCalendarEvent
{
    public DateTimeOffset StartUtc { get; }
    public DateTimeOffset EndUtc { get; }
    public string? OriginalStartTimeZone { get; }
    public string? OriginalEndTimeZone { get; }
    public string? Subject { get; }
    public string? Location { get; }
    public string? OrganizerName { get; }
    public IReadOnlyList<string> AttendeeNames { get; }
    public bool AreAttendeesTruncated { get; }
    public bool IsAllDay { get; }
    public bool IsPrivate { get; }
    public string? Availability { get; }
}

public sealed class Microsoft365WorkContextException : Exception
{
    public WorkContextFacet? Facet { get; }
    public WorkContextFailureKind Kind { get; }
    public System.Net.HttpStatusCode? StatusCode { get; }
    public string? RequestId { get; }
}

public static class Microsoft365WorkContextServiceCollectionExtensions
{
    public static IServiceCollection AddMicrosoft365WorkContext(
        this IServiceCollection services,
        Action<Microsoft365WorkContextOptions> configure);
}
```

The exact placement of types into source folders is not part of the public contract. All public types remain in the root package namespace unless a later spec explicitly approves another consumer-facing namespace.

The module MUST NOT expose public wire DTOs, batch request types, Graph SDK types, raw JSON, a public concrete HTTP client, setters on result models, or public constructors for result models.

## Result Invariants

Every `WorkContextFacetResult<T>` MUST satisfy exactly one of these states:

| Status | Value | Failure | Meaning |
| --- | --- | --- | --- |
| `Disabled` | `null` | `null` | The facet was disabled and no operation was attempted. |
| `Available` | non-null | `null` | The facet was retrieved successfully. An empty calendar list is available data, not unavailable data. |
| `Unavailable` | `null` | `null` | Graph reported an expected absence, such as no manager or no applicable mailbox/calendar resource. |
| `Failed` | null or partial | non-null | Retrieval or validation failed. Work Settings and Calendar MAY preserve successfully mapped partial data in best-effort mode. |

For Work Settings:

- A successful child response contributes its value independently.
- Expected absence of one child setting leaves that property null.
- If at least one child setting succeeds and none fails, the facet is `Available`.
- If every child setting is unavailable and none fails, the facet is `Unavailable`.
- If any child setting fails, the facet is `Failed`. In best-effort mode, `Value` is non-null when at least one child setting succeeded and contains only successfully mapped settings; otherwise it is null.

For Calendar, malformed individual events are omitted in best-effort mode and make the facet `Failed` with a non-null list of the valid events, including an empty list when none are valid. An HTTP- or facet-level Calendar failure has a null `Value`. Fail-fast mode throws instead.

When several operations owned by one facet fail, the exposed `Failure` is selected by the fixed operation order defined below. Failure selection MUST NOT depend on batch response order.

Collections MUST be immutable snapshots. Caller mutation of source options, response arrays, or mutable JSON data MUST NOT change an existing client configuration or snapshot.

## Options and Validation

`Microsoft365WorkContextOptions` MUST be validated and snapshotted when services resolve the client.

- `CalendarLookAhead` MUST be greater than zero and no greater than seven days.
- `MaximumCalendarEvents` MUST be from 1 through 25 inclusive.
- `ErrorBehavior` MUST be a defined enum value.
- Disabling Calendar does not bypass validation of calendar options.
- Disabling every facet is valid. `GetSnapshotAsync` then returns four `Disabled` results without requesting a token or making an HTTP request.

Invalid options MUST fail before token acquisition or network I/O. Later mutations to the configured options object MUST NOT change an existing client.

## Microsoft Graph Operations

The client uses only fixed, package-owned Microsoft Graph v1.0 paths. No path, host, `$select` field, or OData expression comes from model input, end-user text, or host configuration.

### User Profile

When enabled, the canonical Graph operation is:

```http
GET /v1.0/me?$select=displayName,givenName,surname,jobTitle,department,officeLocation,preferredLanguage
```

Map only those seven fields. Do not request or expose `id`, `mail`, `userPrincipalName`, phone numbers, addresses, open extensions, or custom security attributes.

The documented least-privileged delegated permission is `User.Read`.

### Manager

When enabled, request the immediate manager only:

```http
GET /v1.0/me/manager?$select=displayName,jobTitle,department,officeLocation
```

Do not retrieve the management chain, identifiers, email addresses, user principal names, phones, or direct reports. A `404 Not Found` means `Unavailable` rather than `Failed`.

Microsoft's current documentation is inconsistent: the manager operation page lists delegated `User.Read.All`, while the user operation page says `User.Read` permits discovery of the signed-in user's manager. The package documentation MUST surface this discrepancy. The Plan gate MUST establish behavior with real delegated `User.Read` and `User.Read.All` tokens before the permission guidance is finalized.

### Work Settings

When enabled, use three narrow child operations rather than retrieving the complete mailbox settings object:

```http
GET /v1.0/me/mailboxSettings/timeZone
GET /v1.0/me/mailboxSettings/language
GET /v1.0/me/mailboxSettings/workingHours
```

This avoids retrieving unrelated settings such as automatic-reply messages. The documented least-privileged delegated permission is `MailboxSettings.Read`.

Map:

- mailbox time-zone identifier;
- language locale and display name;
- working days, start time, end time, and working-hours time-zone name.

The client MUST accept documented Windows, IANA, and custom time-zone names as opaque strings. It MUST NOT attempt lossy conversion between time-zone systems.

### Calendar

When enabled, request the signed-in user's default calendar view from `CapturedAtUtc` through `CapturedAtUtc + CalendarLookAhead`:

```http
GET /v1.0/me/calendar/calendarView
    ?startDateTime={utc_start}
    &endDateTime={utc_end}
    &$top={MaximumCalendarEvents}
    &$select=subject,start,end,originalStartTimeZone,originalEndTimeZone,
             location,organizer,attendees,isAllDay,isCancelled,
             sensitivity,showAs,responseStatus
```

The actual URL MUST be one correctly escaped relative URL. The multiline form above is explanatory only.

- `startDateTime` and `endDateTime` MUST include explicit UTC offsets and use invariant ISO 8601 formatting.
- No `Prefer: outlook.timezone` header is sent, so Graph returns event start and end values in UTC.
- The client returns at most `MaximumCalendarEvents` and does not follow `@odata.nextLink` in V1.
- Returned events MUST be sorted by `StartUtc`, then `EndUtc`, before snapshot construction.
- Cancelled events and events whose signed-in-user `responseStatus.response` is `declined` are omitted.
- All-day and free events remain; `IsAllDay` and `Availability` preserve those semantics.
- Recurrence instances and exceptions returned by `calendarView` are ordinary events for V1.
- `sensitivity` is privacy-critical. A missing, null, structurally invalid, or unrecognized sensitivity value makes that event invalid; best effort omits it and marks Calendar `Failed`, while fail fast throws.
- A private event keeps start, end, original time zones, all-day state, and availability. Its subject, location, organizer, and attendee names MUST be null or empty, and `AreAttendeesTruncated` MUST be false in the public snapshot.
- For a non-private event, map only location display name, organizer display name, and nonblank attendee display names.
- Keep at most 10 attendee names per event in response order and set `AreAttendeesTruncated` when additional attendee entries exist.
- Organizer and attendee `emailAddress` objects can contain addresses, and a location object can contain an address, coordinates, email address, URI, and identifiers. Minimal wire DTOs MUST declare only organizer/attendee names and location display name, causing `System.Text.Json` to ignore every other nested property.
- Do not request body, body preview, attachments, extensions, online-meeting details, web links, identifiers, categories, or change keys.

The documented least-privileged delegated permission is `Calendars.ReadBasic`. The Plan gate MUST prove that every selected field needed by this contract is returned under `Calendars.ReadBasic`; otherwise the spec must either reduce fields or explicitly approve and document `Calendars.Read` before implementation.

The current official calendar-view reference documents `$top` and says only that the operation supports some OData parameters. It does not explicitly guarantee default chronological order or document `$orderby=start/dateTime`. The Plan gate MUST verify a server-supported chronological query before implementation. The client MUST NOT claim that `$top` returns the nearest events based only on local sorting after server truncation. Cancelled, declined, private-invalid, or malformed entries removed from the bounded server page are not backfilled through paging in V1, so the final list can contain fewer than `MaximumCalendarEvents`.

## Batching and Request Selection

Build a fixed ordered operation list using these stable internal ids:

```text
profile
manager
work-time-zone
work-language
work-hours
calendar
```

- If zero operations are enabled, perform no token or HTTP work.
- If exactly one operation is enabled, issue that direct Graph `GET`.
- If two or more operations are enabled, acquire one token and issue one `POST https://graph.microsoft.com/v1.0/$batch` containing all operations.
- Configure the HTTP client base address as `https://graph.microsoft.com/`. Direct request URIs MUST begin with `v1.0/`; batch subrequest URLs MUST begin with `/me` and are relative to the batch's `v1.0` endpoint. Do not combine a `/me` direct URI with a `/v1.0/` base address.
- Requests are independent and MUST NOT use `dependsOn`.
- The maximum V1 batch contains six operations, below Graph's documented limit of 20.
- Correlate subresponses by id because response order is unspecified.
- Treat a missing or duplicate expected id as an invalid response for the affected operation. Ignore unknown additional ids without exposing their bodies.
- An outer `200 OK` never implies subrequest success; inspect every subresponse status.
- A malformed outer batch request/response affects every requested facet.
- A subrequest `429` is a failure for that facet and preserves `Retry-After` only in internal diagnostics; V1 performs no automatic retry.

The implementation MUST use structured JSON serialization. It MUST NOT concatenate request JSON manually.

## Error Semantics

### Best Effort

`BestEffort` is the default.

- A failed subrequest produces `Failed` only for its owning facet; other facet results remain usable.
- Authentication, token acquisition, transport, malformed outer batch, or outer Graph failures that prevent all operations produce `Failed` for every enabled facet and return a snapshot.
- Expected absence produces `Unavailable`, not `Failed`.
- Cancellation always propagates as `OperationCanceledException` and MUST NOT be converted into a facet failure.
- Invalid local options and programming errors always throw and MUST NOT be converted into a facet failure.

### Fail Fast

`FailFast` throws `Microsoft365WorkContextException` when any enabled operation has a real failure, including token acquisition, transport, authentication, authorization, throttling, unexpected Graph status, or invalid response data.

Expected absence remains `Unavailable` and does not throw. When several batch operations fail, the exception represents one deterministic failure selected in the fixed operation order; exception ordering MUST NOT depend on Graph response order.

The exception and facet failure objects MAY contain:

- owning facet when one can be identified;
- normalized failure kind;
- HTTP status code;
- Graph `request-id`, falling back to `client-request-id`.

They MUST NOT contain tokens, authorization headers, claims, request URLs with personal values, raw response bodies, Graph error messages, profile values, manager values, mailbox values, event values, or attendee values.

### Status Classification

- `401` maps to `Authentication`.
- `403` maps to `Authorization`.
- `429` maps to `Throttled`.
- `5xx` and other unexpected non-success statuses map to `Service`.
- `404` maps to `Unavailable` only for explicitly documented absence cases; it is otherwise `Service`.
- Token-provider exceptions map to `TokenAcquisition` unless they are cancellation.
- A null, empty, or whitespace token is a `TokenAcquisition` failure and MUST be detected before constructing an authorization header or sending HTTP.
- `HttpRequestException` maps to `Transport`.
- Structurally invalid or semantically unusable successful JSON maps to `InvalidResponse`.

`Microsoft365WorkContextException` MUST NOT preserve an unsafe token-provider, HTTP, JSON, or Graph exception as `InnerException`. Internal exception details can contain credentials, claims, endpoints, or personal data and are not part of the public diagnostic contract.

## Authentication and Host Responsibilities

The package consumes one host-acquired delegated Microsoft Graph bearer token for each snapshot operation. `IMicrosoft365WorkContextTokenProvider` is the only identity boundary in this module.

The host owns:

- choosing and configuring the delegated authentication flow;
- acquiring and caching tokens with MSAL, Microsoft Identity Web, Azure Identity, or another appropriate library;
- requesting and consenting the permissions required by enabled facets;
- handling Conditional Access and claims challenges;
- endpoint authentication and business authorization;
- ensuring the token represents the current invocation's user in multi-user hosts;
- preventing singleton token-provider implementations from leaking one user's token into another request.

The package MUST NOT inspect `HttpContext`, parse claims, choose credentials, invoke interactive login, cache tokens, request incremental consent, or fall back to application identity.

## Security and Privacy

### Trust Boundaries and Assets

Trust boundaries are the host-provided token source, Microsoft Graph HTTP responses, and the public snapshot consumer. Protected assets are delegated tokens and personal work data.

The implementation and documentation MUST state:

- Work context is model enrichment, not authorization evidence.
- Snapshot fields MUST NOT grant access, select tools, bypass endpoint policy, or enforce business rules.
- Graph and host authorization remain authoritative.
- Graph strings are untrusted external data and can contain instruction-like content.
- The dependent provider must render these values as quoted data, not trusted system instructions.

### Data Minimization

- Request only the fixed fields in this spec.
- Do not expose raw Graph models, raw JSON, arbitrary metadata, claims, tokens, identifiers, or contact addresses.
- Keep no static, singleton, session, distributed, or persistent Graph-data cache.
- Keep only operation-local data until the immutable snapshot is returned.
- Discard attendee addresses and private-event descriptive fields before public snapshot construction.
- Do not include personal values in logs, metrics dimensions, activity tags, exception messages, or test snapshots committed from live tenants.

### Abuse Cases to Test

- A malicious profile or event subject contains prompt-like instructions, control characters, or very long text.
- A batch response swaps order, duplicates ids, omits ids, or includes unknown ids.
- One user's token provider is accidentally registered as a process-wide mutable singleton.
- A private event contains a revealing title, location, organizer, or attendee list.
- An attendee object contains an address even though only its name is needed.
- Graph returns success with malformed dates, times, enum values, arrays, or object shapes.

The core client validates structure and suppresses forbidden fields. Escaping, length bounding for model context, and instruction/data separation are owned and tested again by `agent-framework-provider`.

## Logging and Observability

Safe structured logs MAY include:

- operation start and completion;
- enabled facet flags or operation count;
- whether batching was used;
- elapsed duration;
- per-facet status;
- HTTP status code and safe request id on failure;
- returned event count and whether attendees were truncated.

Logs MUST NOT include token values, authorization headers, scopes from the token, user or manager names, titles, departments, offices, locale values, time-zone values, working hours, event subjects, locations, organizers, attendees, raw URLs, raw JSON, or Graph error bodies.

Logging failures MUST NOT change snapshot behavior.

## Dependency Injection

`AddMicrosoft365WorkContext` MUST:

- validate `services` and `configure` arguments;
- register and validate snapshotted `Microsoft365WorkContextOptions`;
- register one named or typed `HttpClient` with base address `https://graph.microsoft.com/`;
- register `IMicrosoft365WorkContextClient` with no mutable per-user state;
- use a host-registered `TimeProvider` when available and otherwise use `TimeProvider.System`, without replacing a host registration;
- require the host to register `IMicrosoft365WorkContextTokenProvider` and provide no default token provider;
- avoid changing authentication, authorization, token caches, global HTTP handlers, or unrelated services.

The client can be transient or singleton only if its implementation contains exclusively immutable configuration and stateless dependencies. The Plan MUST choose and justify the lifetime against multi-user ASP.NET Core use; no access token or snapshot may be stored in instance fields.

## Project Structure

```text
src/
  Acterion.Agents.AI.Microsoft365.WorkContext/
    Acterion.Agents.AI.Microsoft365.WorkContext.csproj
    Authentication/
      IMicrosoft365WorkContextTokenProvider.cs
    WorkContext/
      IMicrosoft365WorkContextClient.cs
      Microsoft365WorkContextOptions.cs
      Microsoft365WorkContextServiceCollectionExtensions.cs
      Models/
        WorkContextSnapshot.cs
        WorkContextFacetResult.cs
        WorkContextUserProfile.cs
        WorkContextManager.cs
        WorkContextWorkSettings.cs
        WorkContextCalendarEvent.cs
    Internal/
      [HTTP, batch, validation, mapping, and logging implementation]

tests/
  Acterion.Agents.AI.Microsoft365.WorkContext.Tests/
    Acterion.Agents.AI.Microsoft365.WorkContext.Tests.csproj
    Authentication/
    PublicContract/
    WorkContext/
    TestDoubles/
```

Folders group ownership only; they do not create additional public namespaces. Exact internal filenames are a planning concern.

## Code Style

Follow existing repository conventions: file-scoped namespaces, PascalCase public members, camelCase private fields and locals, nullable annotations, async I/O end to end, explicit cancellation, sealed result types, and XML documentation for public API.

Representative style:

```csharp
namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>
/// Supplies a host-acquired delegated Microsoft Graph access token.
/// </summary>
public interface IMicrosoft365WorkContextTokenProvider
{
    /// <summary>
    /// Gets the delegated token for the current snapshot operation.
    /// </summary>
    Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken = default);
}
```

Prefer straightforward BCL code. Do not add generic repositories, mediators, result frameworks, custom HTTP abstractions, or public Graph-shaped DTOs.

## Testing Strategy

Normal tests MUST require no tenant, credentials, Microsoft 365 license, clock dependency, or network access.

### Public Contract Tests

- Compile consumer-style registration, token-provider, client, options, snapshot, and result usage.
- Assert exact defaults and option bounds.
- Assert result types are sealed, read-only, and not publicly constructible.
- Assert no Graph SDK, identity SDK, ASP.NET Core, raw JSON, or public wire DTO leaks through the public surface.

### Request and Batching Tests

- Assert every exact method, relative URL, `$select`, date range, `$top`, authorization header, and content type.
- Assert zero operations produce no token or HTTP call.
- Assert one operation uses one direct `GET` and two through six operations use one `$batch` request.
- Assert stable unique ids, no `dependsOn`, response correlation independent of order, and no more than six subrequests.
- Assert options and time are snapshotted and UTC date parameters are deterministic with a fake `TimeProvider`.

### Mapping and Privacy Tests

- Cover all profile, manager, locale, working-hours, and calendar fields, including missing optional values and unknown JSON fields.
- Prove user email, UPN, phones, ids, attendee addresses, event body fields, links, and attachments never reach public models.
- Cover private-event redaction, cancelled and declined omission, all-day and free preservation, chronological sorting, attendee truncation, and empty calendar semantics.
- Cover Windows, IANA, and custom time-zone names without conversion.
- Cover malformed individual calendar entries and partial Work Settings values.

### Failure Tests

- Cover direct and batch `401`, `403`, `404`, `429`, `5xx`, transport, token-provider, malformed JSON, missing ids, duplicate ids, unknown ids, and partial successes.
- Assert best effort returns successful facets and sanitized failures.
- Assert fail fast throws only for real failures and uses deterministic operation priority.
- Assert expected absence remains `Unavailable` in both modes.
- Assert cancellation propagates unchanged from token acquisition, send, and response reading.
- Assert exceptions and logs contain no token, personal value, URL payload, Graph error message, or raw body.

### Live Plan-Gate Probes

Live probes are mandatory Plan-gate evidence but are not normal CI tests. They MUST use a disposable test account and synthetic, non-sensitive calendar data. They establish:

- actual `/me/manager` behavior under `User.Read` versus `User.Read.All`;
- selected calendar fields returned under `Calendars.ReadBasic`;
- a supported server-side chronological query for calendar view;
- representative absence responses for users without a manager or mailbox.

No live response body or personal value may be committed as evidence.

## Commands

After the module project exists, use:

```sh
/Users/luisman/.dotnet/dotnet build Acterion.Agents.AI.slnx --configuration Release --no-incremental
/Users/luisman/.dotnet/dotnet test --project tests/Acterion.Agents.AI.Microsoft365.WorkContext.Tests/Acterion.Agents.AI.Microsoft365.WorkContext.Tests.csproj --configuration Release
/Users/luisman/.dotnet/dotnet pack src/Acterion.Agents.AI.Microsoft365.WorkContext/Acterion.Agents.AI.Microsoft365.WorkContext.csproj --configuration Release --no-build --output artifacts/packages
git diff --check
```

Focused Microsoft Testing Platform class filters MUST use an exact fully qualified class name unless the current runner is first proven to support another pattern.

## Boundaries

### Always

- Validate options before token acquisition or HTTP I/O.
- Use the signed-in user's delegated token for every operation.
- Request only fixed Graph v1.0 paths and allowlisted fields.
- Treat Graph responses as untrusted and validate before mapping.
- Use one batch HTTP request whenever two or more operations are enabled.
- Propagate cancellation unchanged.
- Redact private events and discard contact addresses before snapshot construction.
- Preserve explicit per-facet state and immutable snapshot semantics.
- Test every observable public, HTTP, privacy, and error contract.

### Ask First

- Add or remove a public type, property, enum value, or constructor.
- Add a package dependency, especially Microsoft Graph or identity SDKs.
- Broaden a delegated permission or switch from `Calendars.ReadBasic` to `Calendars.Read`.
- Add another Graph field, facet, endpoint, calendar, retry, or paging behavior.
- Change calendar defaults or validation bounds.
- Store, cache, persist, or emit telemetry containing work-context data.

### Never

- Treat snapshot data as authorization, entitlement, identity proof, or a security boundary.
- Acquire credentials, parse claims, cache tokens, or use application permissions.
- Log or expose tokens, claims, raw bodies, Graph errors, personal values, attendee addresses, or private-event descriptions.
- Retrieve complete mailbox settings, calendar bodies, attachments, online-meeting details, or arbitrary Graph fields.
- Add cross-user mutable state, Graph-data caching, conversation memory, or host-specific dependencies.
- Silently rely on undocumented calendar ordering or claim an unverified least-privilege permission.

## Success Criteria

- [ ] The new package and tests build under the repository-pinned .NET 10 SDK with no warnings.
- [ ] A separate consumer-style test compiles the exact public core contract and defaults.
- [ ] Profile-only defaults issue one delegated `GET /me` with the exact allowlist and return an immutable available profile.
- [ ] Any combination producing two through six operations issues one Graph `$batch` request with stable ids and no dependencies.
- [ ] All-disabled configuration returns a fully disabled snapshot without token or network access.
- [ ] Work Settings uses only the three narrow child endpoints and can preserve partial successful values in best-effort mode.
- [ ] Calendar uses a 24-hour default window, retrieves a chronologically ordered server page capped at 10, returns its eligible events in chronological order without paging or backfill, and retrieves only the approved metadata.
- [ ] Cancelled and declined events are absent; all-day and free events remain; private-event descriptive fields are absent.
- [ ] Attendee addresses, user contact fields, raw Graph data, bodies, attachments, links, ids, tokens, and claims never appear in public snapshots, logs, or exceptions.
- [ ] Batch subrequest failures affect only their facet in best-effort mode, while successful sibling facets remain available.
- [ ] Fail-fast mode throws for real failures but not expected absence, and cancellation always propagates unchanged.
- [ ] Profile-only, partial batch, strict failure, malformed response, private calendar, and no-facet paths have credential-free automated tests.
- [ ] Required delegated permissions and known Graph documentation inconsistencies are documented without overstating least privilege.
- [ ] Release build, focused tests, package creation, editor diagnostics, and `git diff --check` pass.

## Plan Gate Exit Evidence

Before implementation tasks are approved, the Plan MUST provide:

1. A compiled public API probe against the centrally pinned dependency graph.
2. Exact serialized direct and six-operation batch requests with encoded URLs and stable ids.
3. A state-transition table covering every facet under disabled, available, unavailable, partial, and failed outcomes in both error modes.
4. A threat-model table mapping token and personal-data disclosure risks to tests and controls.
5. A live synthetic-account result for manager permissions, calendar basic-field coverage, calendar chronological query support, and expected absence responses.
6. A dependency report proving no Graph SDK, Azure Identity, MSAL, Microsoft Identity Web, ASP.NET Core, or new abstraction package was added.
7. A service-lifetime analysis proving no per-user token or snapshot can survive an invocation or cross requests.

If the manager permission, calendar field, ordering, or expected-absence probes contradict this spec, update and reapprove the relevant permission, field, ordering, absence, or limit requirement before implementation. Do not hide the discrepancy in the Plan.

## Open Questions

- None blocking the Specify gate. Manager permission, `Calendars.ReadBasic` field coverage, and calendar chronological query support are deliberately assigned to mandatory Plan-gate evidence because official Microsoft documentation is currently incomplete or inconsistent.

## Authoritative References

- Microsoft Graph get signed-in user: https://learn.microsoft.com/en-us/graph/api/user-get?view=graph-rest-1.0
- Microsoft Graph list manager: https://learn.microsoft.com/en-us/graph/api/user-list-manager?view=graph-rest-1.0
- Microsoft Graph mailbox settings: https://learn.microsoft.com/en-us/graph/api/user-get-mailboxsettings?view=graph-rest-1.0
- Microsoft Graph calendar view: https://learn.microsoft.com/en-us/graph/api/calendar-list-calendarview?view=graph-rest-1.0
- Microsoft Graph event resource: https://learn.microsoft.com/en-us/graph/api/resources/event?view=graph-rest-1.0
- Microsoft Graph attendee resource: https://learn.microsoft.com/en-us/graph/api/resources/attendee?view=graph-rest-1.0
- Microsoft Graph JSON batching: https://learn.microsoft.com/en-us/graph/json-batching
- Microsoft Graph throttling: https://learn.microsoft.com/en-us/graph/throttling
- Microsoft Graph permissions reference: https://learn.microsoft.com/en-us/graph/permissions-reference