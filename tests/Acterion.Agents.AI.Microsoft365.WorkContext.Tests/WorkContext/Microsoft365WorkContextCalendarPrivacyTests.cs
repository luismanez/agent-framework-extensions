using System.Net;
using System.Text.Json;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class Microsoft365WorkContextCalendarPrivacyTests
{
    [Fact]
    public async Task GetSnapshotAsync_PrivateEventKeepsOnlyTimeAndAvailability()
    {
        string responseJson = """
            { "value": [{
              "id": "forbidden-id", "subject": "private-title-canary", "body": { "content": "body-canary" },
              "webLink": "https://example.invalid/link-canary",
              "start": { "dateTime": "2026-09-26T09:00:00", "timeZone": "UTC" },
              "end": { "dateTime": "2026-09-26T10:00:00", "timeZone": "UTC" },
              "originalStartTimeZone": "Custom Start", "originalEndTimeZone": "Custom End",
              "location": { "displayName": "private-location-canary", "address": { "street": "address-canary" } },
              "organizer": { "emailAddress": { "name": "private-organizer-canary", "address": "organizer@example.invalid" } },
              "attendees": [{ "emailAddress": { "name": "private-attendee-canary", "address": "attendee@example.invalid" } }],
              "sensitivity": "private", "isAllDay": true, "showAs": "busy"
            }] }
            """;
        Microsoft365WorkContextClient client = Microsoft365WorkContextCalendarMappingTests.CreateClient(
            new Microsoft365WorkContextCalendarMappingTests.CalendarResponseHandler(responseJson));

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        WorkContextCalendarEvent item = Assert.Single(snapshot.Calendar.Value!);
        Assert.True(item.IsPrivate);
        Assert.True(item.IsAllDay);
        Assert.Equal("busy", item.Availability);
        Assert.Equal("Custom Start", item.OriginalStartTimeZone);
        Assert.Equal("Custom End", item.OriginalEndTimeZone);
        Assert.Null(item.Subject);
        Assert.Null(item.Location);
        Assert.Null(item.OrganizerName);
        Assert.Empty(item.AttendeeNames);
        Assert.False(item.AreAttendeesTruncated);
        string publicJson = JsonSerializer.Serialize(snapshot);
        foreach (string canary in new[] { "private-title-canary", "body-canary", "link-canary", "address-canary", "private-location-canary", "private-organizer-canary", "private-attendee-canary", "forbidden-id", "example.invalid" })
        {
            Assert.DoesNotContain(canary, publicJson);
        }
    }

    [Fact]
    public async Task GetSnapshotAsync_PublicEventCapsAttendeeNamesAndDropsContactFields()
    {
        string attendees = JsonSerializer.Serialize(Enumerable.Range(0, 12).Select(index => new
        {
            emailAddress = new
            {
                name = index == 0 ? "   " : $"Attendee {index}",
                address = $"address{index}@example.invalid",
            },
        }));
        string responseJson = """
            { "value": [{
              "id": "forbidden-id", "subject": "Meeting",
              "start": { "dateTime": "2026-09-26T09:00:00", "timeZone": "UTC" },
              "end": { "dateTime": "2026-09-26T10:00:00", "timeZone": "UTC" },
              "location": { "displayName": "Room A", "address": { "street": "address-canary" }, "locationUri": "link-canary" },
              "organizer": { "emailAddress": { "name": "Organizer", "address": "organizer@example.invalid" } },
              "attendees": ATTENDEES_PLACEHOLDER,
              "sensitivity": "normal", "showAs": "tentative"
            }] }
            """.Replace("ATTENDEES_PLACEHOLDER", attendees, StringComparison.Ordinal);
        Microsoft365WorkContextClient client = Microsoft365WorkContextCalendarMappingTests.CreateClient(
            new Microsoft365WorkContextCalendarMappingTests.CalendarResponseHandler(responseJson));

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        WorkContextCalendarEvent item = Assert.Single(snapshot.Calendar.Value!);
        Assert.False(item.IsPrivate);
        Assert.Equal("Meeting", item.Subject);
        Assert.Equal("Room A", item.Location);
        Assert.Equal("Organizer", item.OrganizerName);
        Assert.Equal(Enumerable.Range(1, 10).Select(index => $"Attendee {index}"), item.AttendeeNames);
        Assert.True(item.AreAttendeesTruncated);
        Assert.True(Assert.IsAssignableFrom<ICollection<string>>(item.AttendeeNames).IsReadOnly);
        string publicJson = JsonSerializer.Serialize(snapshot);
        foreach (string canary in new[] { "forbidden-id", "address-canary", "link-canary", "example.invalid", "Attendee 11" })
        {
            Assert.DoesNotContain(canary, publicJson);
        }
    }

    [Theory]
    [InlineData("null")]
    [InlineData("12")]
    [InlineData("\"unknown\"")]
    public async Task GetSnapshotAsync_InvalidSensitivityOmitsEventAndPreservesValidSibling(string sensitivity)
    {
        string responseJson = $$"""
            { "value": [
              { "subject": "unsafe-canary", "start": { "dateTime": "2026-09-26T08:00:00" }, "end": { "dateTime": "2026-09-26T09:00:00" }, "sensitivity": {{sensitivity}} },
              { "subject": "valid", "start": { "dateTime": "2026-09-26T10:00:00" }, "end": { "dateTime": "2026-09-26T11:00:00" }, "sensitivity": "normal" }
            ] }
            """;
        Microsoft365WorkContextClient client = Microsoft365WorkContextCalendarMappingTests.CreateClient(
            new Microsoft365WorkContextCalendarMappingTests.CalendarResponseHandler(responseJson));

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.Calendar.Status);
        Assert.Equal(WorkContextFailureKind.InvalidResponse, snapshot.Calendar.Failure?.Kind);
        Assert.Equal(HttpStatusCode.OK, snapshot.Calendar.Failure?.StatusCode);
        Assert.Equal("valid", Assert.Single(snapshot.Calendar.Value!).Subject);
        Assert.DoesNotContain("unsafe-canary", JsonSerializer.Serialize(snapshot));
    }

    [Fact]
    public async Task GetSnapshotAsync_MalformedDatesAndShapesReturnFailedEmptyList()
    {
        string responseJson = """
            { "value": [
              { "subject": "missing-sensitivity", "start": { "dateTime": "2026-09-26T08:00:00" }, "end": { "dateTime": "2026-09-26T09:00:00" } },
              { "subject": "bad-date", "start": { "dateTime": "invalid" }, "end": { "dateTime": "2026-09-26T09:00:00" }, "sensitivity": "normal" },
              { "subject": "bad-shape", "start": "invalid", "end": { "dateTime": "2026-09-26T09:00:00" }, "sensitivity": "normal" },
              { "subject": "bad-availability", "start": { "dateTime": "2026-09-26T08:00:00" }, "end": { "dateTime": "2026-09-26T09:00:00" }, "sensitivity": "normal", "showAs": "invalid" }
            ] }
            """;
        Microsoft365WorkContextClient client = Microsoft365WorkContextCalendarMappingTests.CreateClient(
            new Microsoft365WorkContextCalendarMappingTests.CalendarResponseHandler(responseJson));

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.Calendar.Status);
        Assert.Equal(WorkContextFailureKind.InvalidResponse, snapshot.Calendar.Failure?.Kind);
        Assert.Empty(snapshot.Calendar.Value!);
        Assert.True(Assert.IsAssignableFrom<ICollection<WorkContextCalendarEvent>>(snapshot.Calendar.Value).IsReadOnly);
    }

    [Fact]
    public async Task GetSnapshotAsync_CalendarHttpFailureKeepsNullValue()
    {
        string responseJson = """
            { "responses": [
              { "id": "calendar", "status": 403, "body": { "error": { "message": "sensitive-canary" } } },
              { "id": "profile", "status": 200, "body": { "displayName": "Avery" } }
            ] }
            """;
        Microsoft365WorkContextClient client = Microsoft365WorkContextCalendarMappingTests.CreateClient(
            new Microsoft365WorkContextCalendarMappingTests.CalendarResponseHandler(responseJson), enableProfile: true);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.Calendar.Status);
        Assert.Null(snapshot.Calendar.Value);
        Assert.Equal(WorkContextFailureKind.Authorization, snapshot.Calendar.Failure?.Kind);
        Assert.Equal(WorkContextFacetStatus.Available, snapshot.UserProfile.Status);
        Assert.DoesNotContain("sensitive-canary", JsonSerializer.Serialize(snapshot));
    }

    [Fact]
    public async Task GetSnapshotAsync_BatchCalendarPartialFailureKeepsSuccessfulProfile()
    {
        string responseJson = """
            { "responses": [
              { "id": "calendar", "status": 200, "body": { "value": [
                { "subject": "invalid", "sensitivity": "unknown" },
                { "subject": "valid", "start": { "dateTime": "2026-09-26T10:00:00" }, "end": { "dateTime": "2026-09-26T11:00:00" }, "sensitivity": "normal" }
              ] } },
              { "id": "profile", "status": 200, "body": { "displayName": "Avery" } }
            ] }
            """;
        Microsoft365WorkContextCalendarMappingTests.CalendarResponseHandler handler = new(responseJson);
        Microsoft365WorkContextClient client = Microsoft365WorkContextCalendarMappingTests.CreateClient(handler, enableProfile: true);

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(WorkContextFacetStatus.Available, snapshot.UserProfile.Status);
        Assert.Equal("Avery", snapshot.UserProfile.Value?.DisplayName);
        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.Calendar.Status);
        Assert.Equal("valid", Assert.Single(snapshot.Calendar.Value!).Subject);
    }

    [Fact]
    public async Task GetSnapshotAsync_MalformedCalendarEnvelopeFailsWithoutPartialValue()
    {
        Microsoft365WorkContextClient client = Microsoft365WorkContextCalendarMappingTests.CreateClient(
            new Microsoft365WorkContextCalendarMappingTests.CalendarResponseHandler("{ \"value\": {} }"));

        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);

        Assert.Equal(WorkContextFacetStatus.Failed, snapshot.Calendar.Status);
        Assert.Equal(WorkContextFailureKind.InvalidResponse, snapshot.Calendar.Failure?.Kind);
        Assert.Null(snapshot.Calendar.Value);
    }
}
