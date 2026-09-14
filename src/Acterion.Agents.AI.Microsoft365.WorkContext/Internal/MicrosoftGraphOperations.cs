namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal static class MicrosoftGraphOperations
{
    internal static readonly Uri Profile = new(
        "v1.0/me?%24select=displayName%2CgivenName%2Csurname%2CjobTitle%2Cdepartment%2CofficeLocation%2CpreferredLanguage",
        UriKind.Relative);

    internal static readonly Uri Manager = new(
        "v1.0/me/manager?%24select=displayName%2CjobTitle%2Cdepartment%2CofficeLocation",
        UriKind.Relative);
}