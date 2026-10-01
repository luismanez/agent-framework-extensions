using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

public sealed class Microsoft365RetrievalOptionsTests
{
    [Fact]
    public void DataSource_DefaultsToSharePoint()
    {
        Microsoft365RetrievalOptions options = new();

        Assert.Equal(Microsoft365RetrievalDataSource.SharePoint, options.DataSource);
        Assert.Null(options.ExternalItemConnectionIds);
    }

    [Fact]
    public void AddMicrosoft365Retrieval_AcceptsExternalItemWithoutConnectionIds()
    {
        using ServiceProvider serviceProvider = CreateServiceProvider(options =>
            options.DataSource = Microsoft365RetrievalDataSource.ExternalItem);

        Assert.NotNull(serviceProvider.GetRequiredService<IMicrosoft365RetrievalClient>());
    }

    [Theory]
    [InlineData(Microsoft365RetrievalDataSource.SharePoint)]
    [InlineData(Microsoft365RetrievalDataSource.OneDriveBusiness)]
    public void AddMicrosoft365Retrieval_RejectsConnectionIdsForOtherSources(
        Microsoft365RetrievalDataSource dataSource)
    {
        using ServiceProvider serviceProvider = CreateServiceProvider(options =>
        {
            options.DataSource = dataSource;
            options.ExternalItemConnectionIds = ["ContosoIT"];
        });

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IMicrosoft365RetrievalClient>());

        Assert.Contains("ExternalItemConnectionIds", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddMicrosoft365Retrieval_RejectsEmptyConnectionIds()
    {
        using ServiceProvider serviceProvider = CreateServiceProvider(options =>
        {
            options.DataSource = Microsoft365RetrievalDataSource.ExternalItem;
            options.ExternalItemConnectionIds = [];
        });

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IMicrosoft365RetrievalClient>());

        Assert.Contains("ExternalItemConnectionIds", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddMicrosoft365Retrieval_RejectsBlankConnectionId(string? connectionId)
    {
        using ServiceProvider serviceProvider = CreateServiceProvider(options =>
        {
            options.DataSource = Microsoft365RetrievalDataSource.ExternalItem;
            options.ExternalItemConnectionIds = [connectionId!];
        });

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IMicrosoft365RetrievalClient>());

        Assert.Contains("ExternalItemConnectionIds", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_RejectsDuplicateConnectionIdsBeforeTokenAcquisition()
    {
        using HttpClient httpClient = new();
        StubTokenProvider tokenProvider = new();

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() =>
            new Microsoft365RetrievalClient(
                httpClient,
                tokenProvider,
                new Microsoft365RetrievalOptions
                {
                    DataSource = Microsoft365RetrievalDataSource.ExternalItem,
                    ExternalItemConnectionIds = ["ContosoIT", "ContosoIT"],
                }));

        Assert.Contains("ExternalItemConnectionIds", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, tokenProvider.CallCount);
    }

    [Fact]
    public void AddMicrosoft365Retrieval_RejectsUnsupportedDataSourceWhenClientIsResolved()
    {
        using ServiceProvider serviceProvider = CreateServiceProvider(options =>
            options.DataSource = (Microsoft365RetrievalDataSource)123);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IMicrosoft365RetrievalClient>());

        Assert.Contains("DataSource", exception.Message, StringComparison.Ordinal);
    }

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

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void AddMicrosoft365Retrieval_RejectsBlankFilterExpressionWhenClientIsResolved(
        string filterExpression)
    {
        using ServiceProvider serviceProvider = CreateServiceProvider(options =>
            options.FilterExpression = filterExpression);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => serviceProvider.GetRequiredService<IMicrosoft365RetrievalClient>());

        Assert.Contains("FilterExpression", exception.Message, StringComparison.Ordinal);
    }

    private static ServiceProvider CreateServiceProvider(Action<Microsoft365RetrievalOptions> configure)
    {
        ServiceCollection services = new();
        services.AddSingleton<IMicrosoft365RetrievalTokenProvider>(new StubTokenProvider());
        services.AddMicrosoft365Retrieval(configure);
        return services.BuildServiceProvider();
    }
}
