using Acterion.Agents.AI.Microsoft365.Retrieval;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Microsoft365Retrieval.Console.Tests;

public sealed class AzureIdentityRetrievalTokenProviderTests
{
    [Fact]
    public async Task GetAccessTokenAsync_RequestsTheGraphDefaultScopeAndForwardsCancellation()
    {
        RecordingTokenCredential credential = new();
        AzureIdentityRetrievalTokenProvider provider = new(credential);
        using CancellationTokenSource cancellationSource = new();

        string token = await provider.GetAccessTokenAsync(cancellationSource.Token);

        Assert.Equal("delegated-token", token);
        Assert.Equal(["https://graph.microsoft.com/.default"], credential.LastScopes!);
        Assert.Equal(cancellationSource.Token, credential.LastCancellationToken);
    }

    private sealed class RecordingTokenCredential : TokenCredential
    {
        public string[]? LastScopes { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public override AccessToken GetToken(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public override ValueTask<AccessToken> GetTokenAsync(
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            LastScopes = requestContext.Scopes;
            LastCancellationToken = cancellationToken;

            return ValueTask.FromResult(new AccessToken("delegated-token", DateTimeOffset.MaxValue));
        }
    }
}

public sealed class PersistentDeviceCodeCredentialTests
{
    [Fact]
    public async Task AuthenticationRecordStore_RoundTripsTheSelectedAccount()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "authentication-record.json");
        AuthenticationRecord expected = CreateAuthenticationRecord();

        try
        {
            AuthenticationRecordStore store = new(path);

            await store.SaveAsync(expected, TestContext.Current.CancellationToken);
            AuthenticationRecord? actual = await store.LoadAsync(TestContext.Current.CancellationToken);

            Assert.NotNull(actual);
            Assert.Equal(expected.Username, actual.Username);
            Assert.Equal(expected.HomeAccountId, actual.HomeAccountId);
            Assert.Equal(expected.TenantId, actual.TenantId);
            Assert.Equal(expected.ClientId, actual.ClientId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void CreateOptions_ConnectsTheSelectedAccountToTheEncryptedCache()
    {
        AuthenticationRecord authenticationRecord = CreateAuthenticationRecord();

        DeviceCodeCredentialOptions options = PersistentDeviceCodeCredential.CreateOptions(
            "tenant-id",
            "client-id",
            authenticationRecord,
            (_, _) => Task.CompletedTask);

        Assert.Equal("tenant-id", options.TenantId);
        Assert.Equal("client-id", options.ClientId);
        Assert.Same(authenticationRecord, options.AuthenticationRecord);
        Assert.Equal("Acterion.Microsoft365Retrieval.Console", options.TokenCachePersistenceOptions.Name);
        Assert.False(options.TokenCachePersistenceOptions.UnsafeAllowUnencryptedStorage);
    }

    private static AuthenticationRecord CreateAuthenticationRecord()
    {
        const string serializedRecord = """
            {
              "username": "developer@contoso.com",
              "authority": "login.microsoftonline.com",
              "homeAccountId": "object-id.tenant-id",
              "tenantId": "tenant-id",
              "clientId": "client-id",
              "version": "1.0"
            }
            """;
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(serializedRecord));
        return AuthenticationRecord.Deserialize(stream);
    }
}

public sealed class SampleConfigurationTests
{
    [Fact]
    public void FromConfiguration_InRetrievalOnlyMode_DoesNotRequireAzureOpenAI()
    {
        IConfiguration source = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "tenant-id",
                ["MicrosoftEntra:ClientId"] = "client-id",
            })
            .Build();

        SampleConfiguration configuration = SampleConfiguration.FromConfiguration(
            source,
            requireAzureOpenAI: false);

        Assert.Null(configuration.AzureOpenAIEndpoint);
        Assert.Null(configuration.AzureOpenAIDeploymentName);
        Assert.Null(configuration.AzureOpenAITenantId);
        Assert.Equal(Microsoft365RetrievalDataSource.SharePoint, configuration.DataSource);
        Assert.Equal(["title", "author"], configuration.ResourceMetadata);
    }

    [Fact]
    public void FromConfiguration_ReportsEveryMissingRequiredSetting()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => SampleConfiguration.FromConfiguration(new ConfigurationBuilder().Build()));

        Assert.Contains("MicrosoftEntra:TenantId", exception.Message, StringComparison.Ordinal);
        Assert.Contains("MicrosoftEntra:ClientId", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AzureOpenAI:TenantId", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AzureOpenAI:Endpoint", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AzureOpenAI:DeploymentName", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromConfiguration_LoadsHierarchicalSettings()
    {
        IConfiguration source = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "tenant-id",
                ["MicrosoftEntra:ClientId"] = "client-id",
                ["AzureOpenAI:TenantId"] = "model-tenant-id",
                ["AzureOpenAI:Endpoint"] = "https://example.openai.azure.com/",
                ["AzureOpenAI:DeploymentName"] = "chat-deployment",
                ["Microsoft365Retrieval:SharePointSiteUrl"] = "https://contoso.sharepoint.com/sites/test/",
                ["Microsoft365Retrieval:FilterExpression"] = "FileType:pdf",
                ["Microsoft365Retrieval:MaximumNumberOfResults"] = "5",
            })
            .Build();

        SampleConfiguration configuration = SampleConfiguration.FromConfiguration(source);

        Assert.Equal("tenant-id", configuration.TenantId);
        Assert.Equal("client-id", configuration.ClientId);
        Assert.Equal("model-tenant-id", configuration.AzureOpenAITenantId);
        Assert.Equal(new Uri("https://example.openai.azure.com/"), configuration.AzureOpenAIEndpoint);
        Assert.Equal("chat-deployment", configuration.AzureOpenAIDeploymentName);
        Assert.Equal(new Uri("https://contoso.sharepoint.com/sites/test/"), configuration.SharePointSiteUrl);
        Assert.Equal(5, configuration.MaximumNumberOfResults);
        Assert.Equal("FileType:pdf", configuration.RetrievalFilter);
        Assert.Equal("Path:\"https://contoso.sharepoint.com/sites/test/\"", configuration.FilterExpression);
    }

    [Fact]
    public void FromConfiguration_UsesLaterProvidersAsOverrides()
    {
        IConfiguration source = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "base-tenant",
                ["MicrosoftEntra:ClientId"] = "base-client",
                ["AzureOpenAI:TenantId"] = "base-model-tenant",
                ["AzureOpenAI:Endpoint"] = "https://base.openai.azure.com/",
                ["AzureOpenAI:DeploymentName"] = "base-deployment",
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "override-tenant",
                ["MICROSOFT365_RETRIEVAL_FILTER"] = "Path:\"https://contoso.sharepoint.com/\"",
            })
            .Build();

        SampleConfiguration configuration = SampleConfiguration.FromConfiguration(source);

        Assert.Equal("override-tenant", configuration.TenantId);
        Assert.Equal("base-client", configuration.ClientId);
        Assert.Equal("Path:\"https://contoso.sharepoint.com/\"", configuration.RetrievalFilter);
    }

    [Fact]
    public void FromConfiguration_SelectsOneDriveWithOptionalFilter()
    {
        IConfiguration source = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "tenant-id",
                ["MicrosoftEntra:ClientId"] = "client-id",
                ["Microsoft365Retrieval:DataSource"] = "OneDriveBusiness",
                ["Microsoft365Retrieval:FilterExpression"] = "FileType:docx",
                ["MICROSOFT365_RETRIEVAL_FILTER"] = "FileType:pdf",
            })
            .Build();

        SampleConfiguration configuration = SampleConfiguration.FromConfiguration(
            source,
            requireAzureOpenAI: false);

        Assert.Equal(Microsoft365RetrievalDataSource.OneDriveBusiness, configuration.DataSource);
        Assert.Equal("FileType:docx", configuration.FilterExpression);
    }

    [Fact]
    public void FromConfiguration_SelectsOneDriveWithoutFilter()
    {
        IConfiguration source = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "tenant-id",
                ["MicrosoftEntra:ClientId"] = "client-id",
                ["Microsoft365Retrieval:DataSource"] = "OneDriveBusiness",
            })
            .Build();

        SampleConfiguration configuration = SampleConfiguration.FromConfiguration(
            source,
            requireAzureOpenAI: false);

        Assert.Equal(Microsoft365RetrievalDataSource.OneDriveBusiness, configuration.DataSource);
        Assert.Null(configuration.FilterExpression);
    }

    [Fact]
    public void FromConfiguration_SelectsExternalItemWithoutConnectionOrMetadataScope()
    {
        IConfiguration source = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "tenant-id",
                ["MicrosoftEntra:ClientId"] = "client-id",
                ["Microsoft365Retrieval:DataSource"] = "ExternalItem",
            })
            .Build();

        SampleConfiguration configuration = SampleConfiguration.FromConfiguration(
            source, requireAzureOpenAI: false);

        Assert.Equal(Microsoft365RetrievalDataSource.ExternalItem, configuration.DataSource);
        Assert.Null(configuration.ExternalItemConnectionIds);
        Assert.Empty(configuration.ResourceMetadata);
        Assert.Null(configuration.FilterExpression);
    }

    [Fact]
    public void FromConfiguration_LoadsExternalItemIdsAndMetadataInOrder()
    {
        IConfiguration source = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "tenant-id",
                ["MicrosoftEntra:ClientId"] = "client-id",
                ["Microsoft365Retrieval:DataSource"] = "ExternalItem",
                ["Microsoft365Retrieval:ExternalItemConnectionIds:0"] = "ContosoIT",
                ["Microsoft365Retrieval:ExternalItemConnectionIds:1"] = "ContosoHR",
                ["Microsoft365Retrieval:ResourceMetadata:0"] = "title",
            })
            .Build();

        SampleConfiguration configuration = SampleConfiguration.FromConfiguration(
            source, requireAzureOpenAI: false);

        Assert.Equal(["ContosoIT", "ContosoHR"], configuration.ExternalItemConnectionIds);
        Assert.Equal(["title"], configuration.ResourceMetadata);
    }

    [Theory]
    [InlineData("SharePoint")]
    [InlineData("OneDriveBusiness")]
    public void FromConfiguration_RejectsConnectionIdsForOtherSources(string dataSource)
    {
        IConfiguration source = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "tenant-id",
                ["MicrosoftEntra:ClientId"] = "client-id",
                ["Microsoft365Retrieval:DataSource"] = dataSource,
                ["Microsoft365Retrieval:ExternalItemConnectionIds:0"] = "ContosoIT",
            })
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SampleConfiguration.FromConfiguration(source, requireAzureOpenAI: false));

        Assert.Contains("ExternalItemConnectionIds", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromConfiguration_RejectsEmptyConnectionIds()
    {
        IConfiguration source = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "tenant-id",
                ["MicrosoftEntra:ClientId"] = "client-id",
                ["Microsoft365Retrieval:DataSource"] = "ExternalItem",
                ["Microsoft365Retrieval:ExternalItemConnectionIds"] = "",
            })
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SampleConfiguration.FromConfiguration(source, requireAzureOpenAI: false));

        Assert.Contains("ExternalItemConnectionIds", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromConfiguration_RejectsDuplicateConnectionIds()
    {
        IConfiguration source = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "tenant-id",
                ["MicrosoftEntra:ClientId"] = "client-id",
                ["Microsoft365Retrieval:DataSource"] = "ExternalItem",
                ["Microsoft365Retrieval:ExternalItemConnectionIds:0"] = "ContosoIT",
                ["Microsoft365Retrieval:ExternalItemConnectionIds:1"] = "ContosoIT",
            })
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SampleConfiguration.FromConfiguration(source, requireAzureOpenAI: false));

        Assert.Contains("ExternalItemConnectionIds", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromConfiguration_RespectsEmptyMetadataArrayInJson()
    {
        const string json = """
            {
              "MicrosoftEntra": { "TenantId": "tenant-id", "ClientId": "client-id" },
              "Microsoft365Retrieval": {
                "DataSource": "ExternalItem",
                "ResourceMetadata": []
              }
            }
            """;
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
        IConfiguration source = new ConfigurationBuilder().AddJsonStream(stream).Build();

        SampleConfiguration configuration = SampleConfiguration.FromConfiguration(
            source, requireAzureOpenAI: false);

        Assert.Empty(configuration.ResourceMetadata);
    }

    [Fact]
    public void FromConfiguration_RejectsSharePointSiteUrlForExternalItem()
    {
        IConfiguration source = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "tenant-id",
                ["MicrosoftEntra:ClientId"] = "client-id",
                ["Microsoft365Retrieval:DataSource"] = "ExternalItem",
                ["Microsoft365Retrieval:SharePointSiteUrl"] = "https://contoso.sharepoint.com/sites/test/",
            })
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SampleConfiguration.FromConfiguration(source, requireAzureOpenAI: false));

        Assert.Contains("SharePointSiteUrl", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromConfiguration_RejectsSharePointSiteUrlForOneDrive()
    {
        IConfiguration source = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "tenant-id",
                ["MicrosoftEntra:ClientId"] = "client-id",
                ["Microsoft365Retrieval:DataSource"] = "OneDriveBusiness",
                ["Microsoft365Retrieval:SharePointSiteUrl"] = "https://contoso.sharepoint.com/sites/test/",
            })
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SampleConfiguration.FromConfiguration(source, requireAzureOpenAI: false));

        Assert.Contains("SharePointSiteUrl", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromConfiguration_RejectsUnknownDataSource()
    {
        IConfiguration source = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MicrosoftEntra:TenantId"] = "tenant-id",
                ["MicrosoftEntra:ClientId"] = "client-id",
                ["Microsoft365Retrieval:DataSource"] = "OneDrive",
            })
            .Build();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            SampleConfiguration.FromConfiguration(source, requireAzureOpenAI: false));

        Assert.Contains("Microsoft365Retrieval:DataSource", exception.Message, StringComparison.Ordinal);
    }
}

public sealed class SampleCommandLineTests
{
    [Theory]
    [InlineData("--retrieval-only", true)]
    [InlineData("--RETRIEVAL-ONLY", true)]
    [InlineData("retrieval-only", false)]
    [InlineData("--unknown", false)]
    public void IsRetrievalOnly_RecognizesTheExactOption(string argument, bool expected)
    {
        Assert.Equal(expected, SampleCommandLine.IsRetrievalOnly([argument]));
    }
}

public sealed class SampleResultFormattingTests
{
    [Fact]
    public void GetTitle_UsesUrlWhenTitleIsAbsent()
    {
        Assert.Equal(
            "https://contoso.example/item/42",
            SampleResultFormatting.GetTitle(
                new Dictionary<string, JsonElement>(),
                "https://contoso.example/item/42"));
    }

    [Fact]
    public void GetTitle_UsesConnectorTitleWhenPresent()
    {
        Dictionary<string, JsonElement> metadata = new()
        {
            ["title"] = JsonSerializer.SerializeToElement("Corporate VPN"),
        };

        Assert.Equal("Corporate VPN", SampleResultFormatting.GetTitle(metadata, "https://contoso.example/item/42"));
    }
}
