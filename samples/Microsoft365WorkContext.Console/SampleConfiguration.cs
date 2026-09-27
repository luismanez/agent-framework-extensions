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
    public static SampleConfiguration FromConfiguration(
        IConfiguration configuration,
        bool requireAzureOpenAI = true)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string[] required = requireAzureOpenAI
            ? ["MicrosoftEntra:TenantId", "MicrosoftEntra:ClientId", "AzureOpenAI:TenantId",
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

        return new SampleConfiguration(
            configuration["MicrosoftEntra:TenantId"]!,
            configuration["MicrosoftEntra:ClientId"]!,
            configuration["AzureOpenAI:TenantId"],
            endpoint,
            configuration["AzureOpenAI:DeploymentName"],
            configuration.GetValue("Microsoft365WorkContext:EnableUserProfile", true),
            configuration.GetValue("Microsoft365WorkContext:EnableManager", true),
            configuration.GetValue("Microsoft365WorkContext:EnableWorkSettings", true),
            configuration.GetValue("Microsoft365WorkContext:EnableCalendar", true),
            maximumCalendarEvents);
    }
}

internal static class SampleCommandLine
{
    internal static bool IsSnapshotOnly(IEnumerable<string> arguments) =>
        arguments.Any(argument => string.Equals(argument, "--snapshot-only", StringComparison.OrdinalIgnoreCase));
}
