using System.Reflection;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.PublicContract;

public sealed class WorkContextConfigurationContractTests
{
    [Fact]
    public void Enums_ExposeExactlyTheApprovedValues()
    {
        Assert.Equal(["BestEffort", "FailFast"], Enum.GetNames<WorkContextErrorBehavior>());
        Assert.Equal(["UserProfile", "Manager", "WorkSettings", "Calendar"], Enum.GetNames<WorkContextFacet>());
        Assert.Equal(["Disabled", "Available", "Unavailable", "Failed"], Enum.GetNames<WorkContextFacetStatus>());
        Assert.Equal(
            ["TokenAcquisition", "Transport", "Authentication", "Authorization", "Throttled", "Service", "InvalidResponse"],
            Enum.GetNames<WorkContextFailureKind>());
    }

    [Fact]
    public void Options_ExposeApprovedDefaults()
    {
        Microsoft365WorkContextOptions options = new();

        Assert.True(options.EnableUserProfile);
        Assert.False(options.EnableManager);
        Assert.False(options.EnableWorkSettings);
        Assert.False(options.EnableCalendar);
        Assert.Equal(WorkContextErrorBehavior.BestEffort, options.ErrorBehavior);
        Assert.Equal(TimeSpan.FromHours(24), options.CalendarLookAhead);
        Assert.Equal(10, options.MaximumCalendarEvents);
    }

    [Fact]
    public void Options_ExposeOnlyTheApprovedSettableProperties()
    {
        Type optionsType = typeof(Microsoft365WorkContextOptions);
        PropertyInfo[] properties = optionsType.GetProperties();

        Assert.True(optionsType.IsSealed);
        Assert.Equal(
            [
                ("CalendarLookAhead", typeof(TimeSpan)),
                ("EnableCalendar", typeof(bool)),
                ("EnableManager", typeof(bool)),
                ("EnableUserProfile", typeof(bool)),
                ("EnableWorkSettings", typeof(bool)),
                ("ErrorBehavior", typeof(WorkContextErrorBehavior)),
                ("MaximumCalendarEvents", typeof(int)),
            ],
            properties
                .Select(property => (property.Name, property.PropertyType))
                .OrderBy(property => property.Name));
        Assert.All(properties, property => Assert.True(property.SetMethod?.IsPublic));
    }

    [Fact]
    public void Interfaces_ExposeOnlyTheApprovedOperations()
    {
        AssertSingleOptionalCancellationMethod(
            typeof(IMicrosoft365WorkContextTokenProvider),
            "GetAccessTokenAsync",
            typeof(Task<string>));
        AssertSingleOptionalCancellationMethod(
            typeof(IMicrosoft365WorkContextClient),
            "GetSnapshotAsync",
            typeof(Task<WorkContextSnapshot>));
    }

    [Fact]
    public void ConfigurationContracts_AreExposedFromTheRootNamespace()
    {
        Type[] phaseOneTypes =
        [
            typeof(IMicrosoft365WorkContextClient),
            typeof(IMicrosoft365WorkContextTokenProvider),
            typeof(Microsoft365WorkContextOptions),
            typeof(WorkContextErrorBehavior),
            typeof(WorkContextFacet),
            typeof(WorkContextFacetStatus),
            typeof(WorkContextFailureKind),
            typeof(WorkContextSnapshot),
        ];

        Assert.All(
            phaseOneTypes,
            type => Assert.Equal("Acterion.Agents.AI.Microsoft365.WorkContext", type.Namespace));
        Assert.Empty(typeof(WorkContextSnapshot).GetConstructors());
    }

    private static void AssertSingleOptionalCancellationMethod(Type type, string name, Type returnType)
    {
        MethodInfo method = Assert.Single(type.GetMethods());
        ParameterInfo parameter = Assert.Single(method.GetParameters());

        Assert.Equal(name, method.Name);
        Assert.Equal(returnType, method.ReturnType);
        Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
        Assert.True(parameter.IsOptional);
    }
}