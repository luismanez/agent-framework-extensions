# Microsoft 365 Work Context

`Acterion.Agents.AI.Microsoft365.WorkContext` retrieves a fresh, bounded snapshot of the signed-in user's Microsoft 365 work context. It is a standalone .NET 10 client; it does not require Microsoft Agent Framework or an identity SDK.

## Use the client

Register a token provider that obtains a **delegated Microsoft Graph token for the current user**. The host owns authentication, token caching, consent, and claims challenges. In a multi-user host, resolve the work-context client within the current user's scope and ensure the token provider represents that user.

```csharp
using Acterion.Agents.AI.Microsoft365.WorkContext;
using Microsoft.Extensions.DependencyInjection;

services.AddScoped<IMicrosoft365WorkContextTokenProvider, HostGraphTokenProvider>();
services.AddMicrosoft365WorkContext(options =>
{
    options.EnableManager = true;
    options.EnableWorkSettings = true;
    options.EnableCalendar = true;
});

// Resolve from the current user's service scope.
IMicrosoft365WorkContextClient client =
    scope.ServiceProvider.GetRequiredService<IMicrosoft365WorkContextClient>();
WorkContextSnapshot snapshot = await client.GetSnapshotAsync(cancellationToken);

if (snapshot.UserProfile.Status == WorkContextFacetStatus.Available)
{
    string? displayName = snapshot.UserProfile.Value?.DisplayName;
}
```

`HostGraphTokenProvider` stands for your implementation of `IMicrosoft365WorkContextTokenProvider`. Its `GetAccessTokenAsync(CancellationToken)` method must return a delegated Graph access token. The package neither acquires tokens nor provides a default token provider. An optional host `TimeProvider` is used for the capture time and calendar window; otherwise `TimeProvider.System` is used.

## Facets and permissions

The host must request and consent the delegated permissions for the enabled facets. These are the operation-specific least-privileged permissions documented for Microsoft Graph v1.0:

| Facet | Default | Delegated permission | Returned data |
| --- | --- | --- | --- |
| [User Profile](https://learn.microsoft.com/en-us/graph/api/user-get?view=graph-rest-1.0) | Enabled | `User.Read` | Display and work-profile fields only |
| [Manager](https://learn.microsoft.com/en-us/graph/api/user-list-manager?view=graph-rest-1.0) | Disabled | `User.Read.All` | Immediate manager's display and work-profile fields |
| [Work Settings](https://learn.microsoft.com/en-us/graph/api/user-get-mailboxsettings?view=graph-rest-1.0) | Disabled | `MailboxSettings.Read` | Time zone, language, and working hours |
| [Calendar](https://learn.microsoft.com/en-us/graph/api/calendar-list-calendarview?view=graph-rest-1.0) | Disabled | `Calendars.ReadBasic` | Bounded metadata from the default calendar |

Manager's documented permission applies to work or school accounts. The Manager operation does not support personal Microsoft accounts or application permissions. Every package request uses `/me` and requires a signed-in user; app-only access is unsupported. A user without an assigned manager receives `Unavailable` for that facet because Graph documents a `404` response for this case.

Work Settings calls only the `timeZone`, `language`, and `workingHours` child endpoints. Calendar's approved fields were also observed under delegated `Calendars.ReadBasic` in a recorded field-coverage probe; the package does not require the broader `Calendars.Read` permission for this contract. That probe does not establish behavior for other fields or tenants.

## Snapshot behavior

- The default `BestEffort` mode returns a status for every facet. A failed facet can coexist with successful ones; `Failure` contains only a normalized kind, optional HTTP status, and safe request ID. `FailFast` throws `Microsoft365WorkContextException` for real failures. An expected missing manager remains `Unavailable` in either mode.
- Invalid options fail when the client resolves. `CalendarLookAhead` must be greater than zero and at most seven days; `MaximumCalendarEvents` must be 1–25. Disabling all facets returns a disabled snapshot without acquiring a token or calling Graph.
- Calendar defaults to a 24-hour window and a maximum of 10 events. The client reads one bounded page, sorts that page locally, and does not follow pagination or backfill filtered events. Graph does not document a chronological default order for this operation, so the page is **not guaranteed to contain the nearest events** in the window.
- Cancelled and declined events are omitted. All-day and free events remain. Private events retain time and availability but suppress subject, location, organizer, and attendee names. Non-private events expose at most 10 attendee display names; attendee addresses are not returned in the snapshot.
- Every call retrieves a new snapshot. The package does not cache Graph data, retry requests, or store user tokens or snapshots in the client. Cancellation propagates to the caller.

## Security boundary

Work context is **untrusted enrichment, never authorization evidence**. Do not use snapshot values to grant access, choose privileged tools, bypass endpoint policy, or enforce business rules. The host remains responsible for endpoint and business authorization. Graph strings may contain instruction-like content; any model-facing integration must treat them as quoted data with its own escaping and length limits.

The package requests fixed, minimal Graph fields. It does not expose contact addresses, claims, tokens, raw Graph JSON, calendar bodies, attachments, or links through public models. Graph may still transmit nested contact details in an HTTP response; minimal internal mapping discards them before constructing the snapshot. Do not log tokens or raw Graph responses in host diagnostics.
