using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Registers the Microsoft 365 work-context client in a host's service collection.</summary>
public static class Microsoft365WorkContextServiceCollectionExtensions
{
    private static readonly Uri GraphBaseAddress = new("https://graph.microsoft.com/");

    /// <summary>Registers the work-context client and validates its configured options.</summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Configures the enabled facets and retrieval behavior.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddMicrosoft365WorkContext(
        this IServiceCollection services,
        Action<Microsoft365WorkContextOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<Microsoft365WorkContextOptions>().Configure(configure);
        services.AddSingleton<IValidateOptions<Microsoft365WorkContextOptions>, OptionsValidator>();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);

        services.AddHttpClient(nameof(Microsoft365WorkContextClient), client =>
            client.BaseAddress = GraphBaseAddress);
        services.AddTransient<IMicrosoft365WorkContextClient>(provider =>
            new Microsoft365WorkContextClient(
                provider.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(nameof(Microsoft365WorkContextClient)),
                provider.GetRequiredService<IMicrosoft365WorkContextTokenProvider>(),
                provider.GetRequiredService<IOptions<Microsoft365WorkContextOptions>>().Value,
                provider.GetRequiredService<TimeProvider>(),
                provider.GetService<ILogger<Microsoft365WorkContextClient>>()));

        return services;
    }

    private sealed class OptionsValidator : IValidateOptions<Microsoft365WorkContextOptions>
    {
        public ValidateOptionsResult Validate(string? name, Microsoft365WorkContextOptions options)
        {
            try
            {
                Microsoft365WorkContextOptionsValidator.ValidateAndSnapshot(options);
                return ValidateOptionsResult.Success;
            }
            catch (OptionsValidationException exception)
            {
                return ValidateOptionsResult.Fail(exception.Failures);
            }
        }
    }
}
