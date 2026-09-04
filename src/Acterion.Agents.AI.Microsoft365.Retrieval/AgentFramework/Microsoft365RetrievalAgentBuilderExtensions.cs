using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Provides Agent Framework builder extensions for Microsoft 365 retrieval.
/// </summary>
public static class Microsoft365RetrievalAgentBuilderExtensions
{
    /// <summary>
    /// Adds Microsoft 365 retrieval with the specified Agent Framework search behavior.
    /// </summary>
    /// <param name="builder">The agent builder to configure.</param>
    /// <param name="behavior">The time at which Agent Framework performs the search.</param>
    /// <returns>The configured agent builder.</returns>
    public static AIAgentBuilder UseMicrosoft365Retrieval(
        this AIAgentBuilder builder,
        TextSearchProviderOptions.TextSearchBehavior behavior) =>
        builder.UseMicrosoft365Retrieval(new TextSearchProviderOptions { SearchTime = behavior });

    /// <summary>
    /// Adds Microsoft 365 retrieval with the supplied Agent Framework search options.
    /// Retrieved text remains untrusted model context managed by <see cref="TextSearchProvider"/>.
    /// </summary>
    /// <param name="builder">The agent builder to configure.</param>
    /// <param name="options">The Agent Framework text-search options.</param>
    /// <returns>The configured agent builder.</returns>
    public static AIAgentBuilder UseMicrosoft365Retrieval(
        this AIAgentBuilder builder,
        TextSearchProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        if (options.SearchTime == TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling)
        {
            throw new NotSupportedException(
                "OnDemandFunctionCalling is not supported when attaching TextSearchProvider through AIAgentBuilder.UseAIContextProviders.");
        }

        return builder.Use((innerAgent, services) =>
        {
            Microsoft365RetrievalSearch search = services.GetRequiredService<Microsoft365RetrievalSearch>();
            TextSearchProvider provider = new(search.SearchAsync, options);

            return new AIAgentBuilder(innerAgent)
                .UseAIContextProviders(provider)
                .Build(services);
        });
    }
}