using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class Microsoft365RetrievalOptionsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(26)]
    public void AddMicrosoft365Retrieval_RejectsInvalidMaximumResultsWhenClientIsResolved(int maximumResults)
    {
        using ServiceProvider serviceProvider = CreateServiceProvider(options =>
            options.MaximumNumberOfResults = maximumResults);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IMicrosoft365RetrievalClient>());

        Assert.Contains("MaximumNumberOfResults", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMicrosoft365Retrieval_RejectsNullResourceMetadataWhenClientIsResolved()
    {
        using ServiceProvider serviceProvider = CreateServiceProvider(options => options.ResourceMetadata = null!);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IMicrosoft365RetrievalClient>());

        Assert.Contains("ResourceMetadata", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddMicrosoft365Retrieval_RejectsBlankResourceMetadataWhenClientIsResolved(string resourceMetadata)
    {
        using ServiceProvider serviceProvider = CreateServiceProvider(options =>
            options.ResourceMetadata = [resourceMetadata]);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IMicrosoft365RetrievalClient>());

        Assert.Contains("ResourceMetadata", exception.Message, StringComparison.Ordinal);
    }

    private static ServiceProvider CreateServiceProvider(Action<Microsoft365RetrievalOptions> configure)
    {
        ServiceCollection services = new();
        services.AddSingleton<IMicrosoft365RetrievalTokenProvider>(new StubTokenProvider());
        services.AddMicrosoft365Retrieval(configure);
        return services.BuildServiceProvider();
    }
}
