using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Adds Microsoft 365 work context to an Agent Framework chat-client pipeline.</summary>
public static class Microsoft365WorkContextChatClientBuilderExtensions
{
    /// <summary>Adds fresh, transient work context to each agent invocation.</summary>
    /// <param name="builder">The chat-client builder.</param>
    /// <returns>The configured builder.</returns>
    public static ChatClientBuilder UseMicrosoft365WorkContext(this ChatClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Use((innerClient, services) =>
            new ChatClientBuilder(innerClient)
                .UseAIContextProviders(services.GetRequiredService<Microsoft365WorkContextProvider>())
                .Build(services));
    }
}
