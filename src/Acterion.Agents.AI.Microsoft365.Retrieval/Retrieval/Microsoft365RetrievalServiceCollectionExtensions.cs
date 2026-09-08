using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Provides dependency-injection registration for Microsoft 365 retrieval services.
/// </summary>
public static class Microsoft365RetrievalServiceCollectionExtensions
{
    private static readonly Uri GraphBaseAddress = new("https://graph.microsoft.com/");

    /// <summary>
    /// Registers the Microsoft 365 retrieval client and validates its configured options.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Configures retrieval request options.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddMicrosoft365Retrieval(
        this IServiceCollection services,
        Action<Microsoft365RetrievalOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services
            .AddOptions<Microsoft365RetrievalOptions>()
            .Configure(configure);
        services.AddSingleton<IValidateOptions<Microsoft365RetrievalOptions>, Microsoft365RetrievalOptionsValidator>();

        services.AddHttpClient(nameof(Microsoft365RetrievalClient), httpClient =>
        {
            httpClient.BaseAddress = GraphBaseAddress;
        });
        services.AddTransient<IMicrosoft365RetrievalClient>(serviceProvider => new Microsoft365RetrievalClient(
            serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(Microsoft365RetrievalClient)),
            serviceProvider.GetRequiredService<IMicrosoft365RetrievalTokenProvider>(),
            serviceProvider.GetRequiredService<IOptions<Microsoft365RetrievalOptions>>().Value,
            serviceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Microsoft365RetrievalClient>>()));
        services.AddTransient<Microsoft365RetrievalSearch>();

        return services;
    }
}
