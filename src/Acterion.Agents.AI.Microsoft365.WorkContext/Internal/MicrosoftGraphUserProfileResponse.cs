using System.Text.Json.Serialization;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal sealed class MicrosoftGraphUserProfileResponse
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("givenName")]
    public string? GivenName { get; init; }

    [JsonPropertyName("surname")]
    public string? Surname { get; init; }

    [JsonPropertyName("jobTitle")]
    public string? JobTitle { get; init; }

    [JsonPropertyName("department")]
    public string? Department { get; init; }

    [JsonPropertyName("officeLocation")]
    public string? OfficeLocation { get; init; }

    [JsonPropertyName("preferredLanguage")]
    public string? PreferredLanguage { get; init; }
}