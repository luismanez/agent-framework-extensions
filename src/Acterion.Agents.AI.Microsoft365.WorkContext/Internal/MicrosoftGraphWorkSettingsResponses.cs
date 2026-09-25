using System.Text.Json.Serialization;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal sealed class MicrosoftGraphLocaleInfoResponse
{
    [JsonPropertyName("locale")]
    public string? Locale { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }
}

internal sealed class MicrosoftGraphWorkingHoursResponse
{
    [JsonPropertyName("daysOfWeek")]
    public IReadOnlyList<string>? DaysOfWeek { get; init; }

    [JsonPropertyName("startTime")]
    public string? StartTime { get; init; }

    [JsonPropertyName("endTime")]
    public string? EndTime { get; init; }

    [JsonPropertyName("timeZone")]
    public MicrosoftGraphTimeZoneBaseResponse? TimeZone { get; init; }
}

internal sealed class MicrosoftGraphTimeZoneBaseResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
