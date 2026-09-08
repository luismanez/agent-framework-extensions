using System.Reflection;
using Acterion.Agents.AI.Microsoft365.Retrieval;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests.PublicContract;

public sealed class SharePointRetrievalFilterPublicContractTests
{
    [Fact]
    public void PublicSurface_ExposesOnlyTheSpecifiedImmutableFactories()
    {
        Type filterType = typeof(SharePointRetrievalFilter);

        Assert.True(filterType.IsSealed);
        Assert.Empty(filterType.GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        PropertyInfo expression = Assert.Single(filterType.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        Assert.Equal(nameof(SharePointRetrievalFilter.Expression), expression.Name);
        Assert.True(expression.CanRead);
        Assert.False(expression.CanWrite);

        MethodInfo[] factoryMethods = filterType
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .OrderBy(method => method.Name)
            .ToArray();

        Assert.Equal(
            [
                "AllOf",
                "AnyOf",
                "Author",
                "FileExtension",
                "FileExtensions",
                "FileName",
                "FileType",
                "InformationProtectionLabelId",
                "LastModifiedBetween",
                "LastModifiedOnOrAfter",
                "LastModifiedOnOrBefore",
                "ModifiedBy",
                "Not",
                "Path",
                "SiteId",
                "Title",
            ],
            factoryMethods.Select(method => method.Name));
        Assert.Equal(typeof(Uri), Assert.Single(factoryMethods.Single(method => method.Name == "Path").GetParameters()).ParameterType);
        Assert.Equal(typeof(Guid), Assert.Single(factoryMethods.Single(method => method.Name == "SiteId").GetParameters()).ParameterType);
        Assert.True(Assert.Single(factoryMethods.Single(method => method.Name == "AnyOf").GetParameters()).GetCustomAttribute<ParamArrayAttribute>() is not null);
        Assert.DoesNotContain(filterType.GetMethods(BindingFlags.Public | BindingFlags.Static), method =>
            method.ReturnType == typeof(string));
    }
}