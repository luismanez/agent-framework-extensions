using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.WorkContext;

public sealed class Microsoft365WorkContextCalendarRequestTests
{
    [Fact]
    public void CreateCalendarUri_UsesInvariantEscapedBoundedQuery()
    {
        DateTimeOffset capturedAtUtc = DateTimeOffset.Parse(
            "2026-09-13T12:34:56.0000000+00:00",
            System.Globalization.CultureInfo.InvariantCulture);

        Uri uri = MicrosoftGraphOperations.CreateCalendarUri(
            capturedAtUtc,
            TimeSpan.FromHours(24),
            maximumCalendarEvents: 10);

        Assert.Equal(
            "v1.0/me/calendar/calendarView?startDateTime=2026-09-13T12%3A34%3A56.0000000%2B00%3A00" +
            "&endDateTime=2026-09-14T12%3A34%3A56.0000000%2B00%3A00" +
            "&%24top=10" +
            "&%24select=subject%2Cstart%2Cend%2CoriginalStartTimeZone%2CoriginalEndTimeZone%2C" +
            "location%2Corganizer%2Cattendees%2CisAllDay%2CisCancelled%2Csensitivity%2CshowAs%2CresponseStatus",
            uri.OriginalString);
        Assert.DoesNotContain("orderby", uri.OriginalString, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("outlook.timezone", uri.OriginalString, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("body", uri.OriginalString, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("id", uri.OriginalString, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onlineMeeting", uri.OriginalString, StringComparison.OrdinalIgnoreCase);
    }
}