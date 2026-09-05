using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Provides chat-client builder extensions for Microsoft 365 retrieval.
/// </summary>
public static class Microsoft365RetrievalChatClientBuilderExtensions
{
    /// <summary>
    /// Adds Microsoft 365 retrieval with the specified Agent Framework search behavior.
    /// </summary>
    /// <param name="builder">The chat-client builder to configure.</param>
    /// <param name="behavior">
    /// The time at which Agent Framework performs the search. For on-demand search, create the resulting agent
    /// with <see cref="ChatClientAgentOptions.UseProvidedChatClientAsIs"/> set to <see langword="true"/>.
    /// </param>
    /// <returns>The configured chat-client builder.</returns>
    public static ChatClientBuilder UseMicrosoft365Retrieval(
        this ChatClientBuilder builder,
        TextSearchProviderOptions.TextSearchBehavior behavior) =>
        builder.UseMicrosoft365Retrieval(new TextSearchProviderOptions { SearchTime = behavior });

    /// <summary>
    /// Adds Microsoft 365 retrieval with the supplied Agent Framework search options.
    /// Retrieved text remains untrusted model context managed by <see cref="TextSearchProvider"/>.
    /// For on-demand retrieval, the configured pipeline includes native function invocation and the resulting
    /// agent must use the chat client as is. This disables Agent Framework's default agent decorators, so hosts
    /// must explicitly compose any additional decorators they require.
    /// </summary>
    /// <param name="builder">The chat-client builder to configure.</param>
    /// <param name="options">The Agent Framework text-search options.</param>
    /// <returns>The configured chat-client builder.</returns>
    public static ChatClientBuilder UseMicrosoft365Retrieval(
        this ChatClientBuilder builder,
        TextSearchProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        return builder.Use((innerClient, services) =>
        {
            Microsoft365RetrievalSearch search = services.GetRequiredService<Microsoft365RetrievalSearch>();
            TextSearchProvider provider = new(search.SearchAsync, options);
            ChatClientBuilder retrievalBuilder = new ChatClientBuilder(innerClient)
                .UseAIContextProviders(provider);

            if (options.SearchTime == TextSearchProviderOptions.TextSearchBehavior.OnDemandFunctionCalling)
            {
                retrievalBuilder.UseFunctionInvocation();
            }

            return retrievalBuilder.Build(services);
        });
    }
}