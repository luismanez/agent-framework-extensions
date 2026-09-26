using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.PublicContract;

public sealed class WorkContextPublicContractTests
{
    [Fact]
    public async Task Consumer_CanRegisterConfigureAndReadEveryPublicMember()
    {
        ServiceCollection services = new();
        services.AddSingleton<IMicrosoft365WorkContextTokenProvider, ConsumerTokenProvider>();
        IServiceCollection registered = services.AddMicrosoft365WorkContext(options =>
        {
            options.EnableUserProfile = false;
            options.EnableManager = false;
            options.EnableWorkSettings = false;
            options.EnableCalendar = false;
            options.ErrorBehavior = WorkContextErrorBehavior.BestEffort;
            options.CalendarLookAhead = TimeSpan.FromHours(24);
            options.MaximumCalendarEvents = 10;
        });
        Assert.Same(services, registered);
        using ServiceProvider provider = services.BuildServiceProvider();

        IMicrosoft365WorkContextClient client = provider.GetRequiredService<IMicrosoft365WorkContextClient>();
        WorkContextSnapshot snapshot = await client.GetSnapshotAsync(TestContext.Current.CancellationToken);
        _ = snapshot.CapturedAtUtc;
        ReadFacet(snapshot.UserProfile, ReadProfile);
        ReadFacet(snapshot.Manager, ReadManager);
        ReadFacet(snapshot.WorkSettings, ReadSettings);
        ReadFacet(snapshot.Calendar, events =>
        {
            foreach (WorkContextCalendarEvent calendarEvent in events)
            {
                _ = calendarEvent.StartUtc;
                _ = calendarEvent.EndUtc;
                _ = calendarEvent.OriginalStartTimeZone;
                _ = calendarEvent.OriginalEndTimeZone;
                _ = calendarEvent.Subject;
                _ = calendarEvent.Location;
                _ = calendarEvent.OrganizerName;
                _ = calendarEvent.AttendeeNames;
                _ = calendarEvent.AreAttendeesTruncated;
                _ = calendarEvent.IsAllDay;
                _ = calendarEvent.IsPrivate;
                _ = calendarEvent.Availability;
            }
        });

        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.UserProfile.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Manager.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.WorkSettings.Status);
        Assert.Equal(WorkContextFacetStatus.Disabled, snapshot.Calendar.Status);
    }

    [Fact]
    public void ExtensionAndExportedSurface_MatchTheApprovedContract()
    {
        Type extension = typeof(Microsoft365WorkContextServiceCollectionExtensions);
        Assert.Equal("Acterion.Agents.AI.Microsoft365.WorkContext", extension.Namespace);
        Assert.True(extension.IsAbstract && extension.IsSealed);
        MethodInfo method = Assert.Single(extension.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly));
        Assert.Equal(nameof(Microsoft365WorkContextServiceCollectionExtensions.AddMicrosoft365WorkContext), method.Name);
        Assert.Equal(typeof(IServiceCollection), method.ReturnType);
        Assert.NotNull(method.GetCustomAttribute<ExtensionAttribute>());
        Assert.Equal(
            [typeof(IServiceCollection), typeof(Action<Microsoft365WorkContextOptions>)],
            method.GetParameters().Select(parameter => parameter.ParameterType));

        Type[] exported = extension.Assembly.GetExportedTypes();
        Assert.All(exported, type => Assert.Equal(extension.Namespace, type.Namespace));
        Assert.DoesNotContain(exported, type => type.FullName!.Contains("Graph", StringComparison.OrdinalIgnoreCase)
            || type.FullName.Contains("Identity", StringComparison.OrdinalIgnoreCase)
            || type.FullName.Contains("Json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Consumer_CanInspectSanitizedFailFastException()
    {
        ServiceCollection services = new();
        services.AddSingleton<IMicrosoft365WorkContextTokenProvider, ThrowingTokenProvider>();
        services.AddMicrosoft365WorkContext(options => options.ErrorBehavior = WorkContextErrorBehavior.FailFast);
        using ServiceProvider provider = services.BuildServiceProvider();

        Microsoft365WorkContextException exception = await Assert.ThrowsAsync<Microsoft365WorkContextException>(
            () => provider.GetRequiredService<IMicrosoft365WorkContextClient>()
                .GetSnapshotAsync(TestContext.Current.CancellationToken));

        Assert.Null(exception.Facet);
        Assert.Equal(WorkContextFailureKind.TokenAcquisition, exception.Kind);
        Assert.Null(exception.StatusCode);
        Assert.Null(exception.RequestId);
    }

    [Fact]
    public void PublicTypesAndDeclaredMembers_HaveXmlDocumentation()
    {
        Type[] exported = typeof(WorkContextSnapshot).Assembly.GetExportedTypes();
        string xmlPath = Path.ChangeExtension(exported[0].Assembly.Location, ".xml");
        XDocument xml = XDocument.Load(xmlPath);
        HashSet<string> documented = xml.Descendants("member")
            .Select(member => (string?)member.Attribute("name"))
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

        foreach (Type type in exported)
        {
            Assert.Contains($"T:{type.FullName}", documented);
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                Assert.Contains($"P:{type.FullName}.{property.Name}", documented);
            }

            if (type.IsEnum)
            {
                foreach (FieldInfo value in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    Assert.Contains($"F:{type.FullName}.{value.Name}", documented);
                }
            }
        }

        Assert.Contains(documented, name => name.StartsWith(
            $"M:{typeof(Microsoft365WorkContextServiceCollectionExtensions).FullName}.AddMicrosoft365WorkContext(",
            StringComparison.Ordinal));
    }

    private static void ReadFacet<T>(WorkContextFacetResult<T> result, Action<T> read) where T : class
    {
        _ = result.Status;
        if (result.Value is { } value)
        {
            read(value);
        }

        if (result.Failure is { } failure)
        {
            _ = failure.Kind;
            _ = failure.StatusCode;
            _ = failure.RequestId;
        }
    }

    private static void ReadProfile(WorkContextUserProfile profile)
    {
        _ = profile.DisplayName;
        _ = profile.GivenName;
        _ = profile.Surname;
        _ = profile.JobTitle;
        _ = profile.Department;
        _ = profile.OfficeLocation;
        _ = profile.PreferredLanguage;
    }

    private static void ReadManager(WorkContextManager manager)
    {
        _ = manager.DisplayName;
        _ = manager.JobTitle;
        _ = manager.Department;
        _ = manager.OfficeLocation;
    }

    private static void ReadSettings(WorkContextWorkSettings settings)
    {
        _ = settings.TimeZone;
        if (settings.Language is { } language)
        {
            _ = language.Locale;
            _ = language.DisplayName;
        }

        if (settings.WorkingHours is { } hours)
        {
            _ = hours.DaysOfWeek;
            _ = hours.StartTime;
            _ = hours.EndTime;
            _ = hours.TimeZone;
        }
    }

    private sealed class ConsumerTokenProvider : IMicrosoft365WorkContextTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("unused-token");
    }

    private sealed class ThrowingTokenProvider : IMicrosoft365WorkContextTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Synthetic token failure.");
    }
}
