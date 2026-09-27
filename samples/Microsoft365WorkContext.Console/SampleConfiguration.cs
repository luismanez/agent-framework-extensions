using Microsoft.Extensions.Configuration;

public sealed record SampleConfiguration(
    string TenantId,
    string ClientId,
    string? AzureOpenAITenantId,
    Uri? AzureOpenAIEndpoint,
    string? AzureOpenAIDeploymentName,
    bool EnableUserProfile,
    bool EnableManager,
    bool EnableWorkSettings,
    bool EnableCalendar,
    int MaximumCalendarEvents)
{
    public string[] GetGraphScopes()
    {
        List<string> scopes = [];
        if (EnableUserProfile)
        {
            scopes.Add("https://graph.microsoft.com/User.Read");
        }

        if (EnableManager)
        {
            scopes.Add("https://graph.microsoft.com/User.Read.All");
        }

        if (EnableWorkSettings)
        {
            scopes.Add("https://graph.microsoft.com/MailboxSettings.Read");
        }

        if (EnableCalendar)
        {
            scopes.Add("https://graph.microsoft.com/Calendars.ReadBasic");
        }

        return scopes.ToArray();
    }

    public static SampleConfiguration FromConfiguration(
        IConfiguration configuration,
        bool requireAzureOpenAI = true)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string[] required = requireAzureOpenAI
            ? ["MicrosoftEntra:TenantId", "MicrosoftEntra:ClientId",
                "AzureOpenAI:Endpoint", "AzureOpenAI:DeploymentName"]
            : ["MicrosoftEntra:TenantId", "MicrosoftEntra:ClientId"];
        string[] missing = required.Where(key => string.IsNullOrWhiteSpace(configuration[key])).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"Missing required settings: {string.Join(", ", missing)}.");
        }

        Uri? endpoint = null;
        string? endpointValue = configuration["AzureOpenAI:Endpoint"];
        if (!string.IsNullOrWhiteSpace(endpointValue) &&
            (!Uri.TryCreate(endpointValue, UriKind.Absolute, out endpoint) ||
             endpoint.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("AzureOpenAI:Endpoint must be an absolute HTTPS URI.");
        }

        int maximumCalendarEvents = configuration.GetValue("Microsoft365WorkContext:MaximumCalendarEvents", 5);
        if (maximumCalendarEvents is < 1 or > 25)
        {
            throw new InvalidOperationException("Microsoft365WorkContext:MaximumCalendarEvents must be between 1 and 25.");
        }

        SampleConfiguration result = new(
            configuration["MicrosoftEntra:TenantId"]!,
            configuration["MicrosoftEntra:ClientId"]!,
            string.IsNullOrWhiteSpace(configuration["AzureOpenAI:TenantId"])
                ? null
                : configuration["AzureOpenAI:TenantId"],
            endpoint,
            configuration["AzureOpenAI:DeploymentName"],
            configuration.GetValue("Microsoft365WorkContext:EnableUserProfile", true),
            configuration.GetValue("Microsoft365WorkContext:EnableManager", true),
            configuration.GetValue("Microsoft365WorkContext:EnableWorkSettings", true),
            configuration.GetValue("Microsoft365WorkContext:EnableCalendar", true),
            maximumCalendarEvents);

        if (result.GetGraphScopes().Length == 0)
        {
            throw new InvalidOperationException("Enable at least one Microsoft365WorkContext facet.");
        }

        return result;
    }
}

internal static class SampleCommandLine
{
    internal static bool IsSnapshotOnly(IEnumerable<string> arguments) =>
        arguments.Any(argument => string.Equals(argument, "--snapshot-only", StringComparison.OrdinalIgnoreCase));
}
