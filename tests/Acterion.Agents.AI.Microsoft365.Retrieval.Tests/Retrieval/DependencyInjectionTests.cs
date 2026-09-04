using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddMicrosoft365Retrieval_RegistersClientWithoutReplacingHostTokenProvider()
    {
        ServiceCollection services = new();
        StubTokenProvider tokenProvider = new();
        services.AddSingleton<IMicrosoft365RetrievalTokenProvider>(tokenProvider);
        services.AddMicrosoft365Retrieval(options => options.MaximumNumberOfResults = 12);
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        IMicrosoft365RetrievalClient client = serviceProvider.GetRequiredService<IMicrosoft365RetrievalClient>();
        IMicrosoft365RetrievalTokenProvider resolvedTokenProvider =
            serviceProvider.GetRequiredService<IMicrosoft365RetrievalTokenProvider>();
        Microsoft365RetrievalOptions options =
            serviceProvider.GetRequiredService<IOptions<Microsoft365RetrievalOptions>>().Value;

        Assert.IsType<Microsoft365RetrievalClient>(client);
        Assert.Same(tokenProvider, resolvedTokenProvider);
        Assert.Equal(12, options.MaximumNumberOfResults);
    }

    [Fact]
    public void AddMicrosoft365Retrieval_RequiresHostTokenProviderWhenClientIsResolved()
    {
        ServiceCollection services = new();
        services.AddMicrosoft365Retrieval(_ => { });
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(
            () => serviceProvider.GetRequiredService<IMicrosoft365RetrievalClient>());
    }
}
