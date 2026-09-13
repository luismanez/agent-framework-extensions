using System.Reflection;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.PublicContract;

public sealed class WorkContextModelContractTests
{
    [Fact]
    public void UserProfile_ExposesOnlyAllowlistedValues()
    {
        AssertReadOnlyContract(
            typeof(WorkContextUserProfile),
            ("Department", typeof(string)),
            ("DisplayName", typeof(string)),
            ("GivenName", typeof(string)),
            ("JobTitle", typeof(string)),
            ("OfficeLocation", typeof(string)),
            ("PreferredLanguage", typeof(string)),
            ("Surname", typeof(string)));

        WorkContextUserProfile profile = Create<WorkContextUserProfile>(
            "display",
            "given",
            "surname",
            "title",
            "department",
            "office",
            "language");

        Assert.Equal("display", profile.DisplayName);
        Assert.Equal("given", profile.GivenName);
        Assert.Equal("surname", profile.Surname);
        Assert.Equal("title", profile.JobTitle);
        Assert.Equal("department", profile.Department);
        Assert.Equal("office", profile.OfficeLocation);
        Assert.Equal("language", profile.PreferredLanguage);
    }

    [Fact]
    public void Manager_ExposesOnlyAllowlistedValues()
    {
        AssertReadOnlyContract(
            typeof(WorkContextManager),
            ("Department", typeof(string)),
            ("DisplayName", typeof(string)),
            ("JobTitle", typeof(string)),
            ("OfficeLocation", typeof(string)));

        WorkContextManager manager = Create<WorkContextManager>(
            "display",
            "title",
            "department",
            "office");

        Assert.Equal("display", manager.DisplayName);
        Assert.Equal("title", manager.JobTitle);
        Assert.Equal("department", manager.Department);
        Assert.Equal("office", manager.OfficeLocation);
    }

    [Fact]
    public void WorkSettingsAndLocale_ExposeOnlyApprovedValues()
    {
        AssertReadOnlyContract(
            typeof(WorkContextWorkSettings),
            ("Language", typeof(WorkContextLocale)),
            ("TimeZone", typeof(string)),
            ("WorkingHours", typeof(WorkContextWorkingHours)));
        AssertReadOnlyContract(
            typeof(WorkContextLocale),
            ("DisplayName", typeof(string)),
            ("Locale", typeof(string)));

        WorkContextLocale locale = Create<WorkContextLocale>("en-GB", "English (United Kingdom)");
        WorkContextWorkSettings settings = Create<WorkContextWorkSettings>("Custom/Zone", locale, null);

        Assert.Equal("Custom/Zone", settings.TimeZone);
        Assert.Same(locale, settings.Language);
        Assert.Null(settings.WorkingHours);
    }

    [Fact]
    public void WorkingHours_CopiesDaysAndPreservesOpaqueTimeZone()
    {
        AssertReadOnlyContract(
            typeof(WorkContextWorkingHours),
            ("DaysOfWeek", typeof(IReadOnlyList<DayOfWeek>)),
            ("EndTime", typeof(TimeOnly)),
            ("StartTime", typeof(TimeOnly)),
            ("TimeZone", typeof(string)));

        DayOfWeek[] sourceDays = [DayOfWeek.Monday, DayOfWeek.Wednesday];
        WorkContextWorkingHours workingHours = Create<WorkContextWorkingHours>(
            sourceDays,
            new TimeOnly(8, 30),
            new TimeOnly(17, 15),
            "Opaque custom zone");
        sourceDays[0] = DayOfWeek.Sunday;

        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Wednesday], workingHours.DaysOfWeek);
        Assert.Equal(new TimeOnly(8, 30), workingHours.StartTime);
        Assert.Equal(new TimeOnly(17, 15), workingHours.EndTime);
        Assert.Equal("Opaque custom zone", workingHours.TimeZone);
        Assert.Throws<NotSupportedException>(() =>
        {
            ((ICollection<DayOfWeek>)workingHours.DaysOfWeek).Add(DayOfWeek.Friday);
        });
    }

    [Fact]
    public void CalendarEvent_ExposesOnlyApprovedValuesAndCopiesAttendees()
    {
        AssertReadOnlyContract(
            typeof(WorkContextCalendarEvent),
            ("AreAttendeesTruncated", typeof(bool)),
            ("AttendeeNames", typeof(IReadOnlyList<string>)),
            ("Availability", typeof(string)),
            ("EndUtc", typeof(DateTimeOffset)),
            ("IsAllDay", typeof(bool)),
            ("IsPrivate", typeof(bool)),
            ("Location", typeof(string)),
            ("OrganizerName", typeof(string)),
            ("OriginalEndTimeZone", typeof(string)),
            ("OriginalStartTimeZone", typeof(string)),
            ("StartUtc", typeof(DateTimeOffset)),
            ("Subject", typeof(string)));

        string[] sourceAttendees = ["First", "Second"];
        WorkContextCalendarEvent calendarEvent = Create<WorkContextCalendarEvent>(
            DateTimeOffset.Parse("2026-09-14T08:00:00Z"),
            DateTimeOffset.Parse("2026-09-14T08:30:00Z"),
            "Start zone",
            "End zone",
            "Subject",
            "Location",
            "Organizer",
            sourceAttendees,
            true,
            false,
            false,
            "busy");
        sourceAttendees[0] = "Mutated";

        Assert.Equal(["First", "Second"], calendarEvent.AttendeeNames);
        Assert.Throws<NotSupportedException>(() =>
        {
            ((ICollection<string>)calendarEvent.AttendeeNames).Add("Third");
        });
    }

    [Fact]
    public void Snapshot_ExposesExactlyFourFacetResultsAndCaptureTime()
    {
        AssertReadOnlyContract(
            typeof(WorkContextSnapshot),
            ("Calendar", typeof(WorkContextFacetResult<IReadOnlyList<WorkContextCalendarEvent>>)),
            ("CapturedAtUtc", typeof(DateTimeOffset)),
            ("Manager", typeof(WorkContextFacetResult<WorkContextManager>)),
            ("UserProfile", typeof(WorkContextFacetResult<WorkContextUserProfile>)),
            ("WorkSettings", typeof(WorkContextFacetResult<WorkContextWorkSettings>)));

        DateTimeOffset capturedAtUtc = DateTimeOffset.Parse("2026-09-13T12:34:56Z");
        WorkContextFacetResult<WorkContextUserProfile> profile = CreateResult<WorkContextUserProfile>();
        WorkContextFacetResult<WorkContextManager> manager = CreateResult<WorkContextManager>();
        WorkContextFacetResult<WorkContextWorkSettings> settings = CreateResult<WorkContextWorkSettings>();
        WorkContextFacetResult<IReadOnlyList<WorkContextCalendarEvent>> calendar =
            CreateResult<IReadOnlyList<WorkContextCalendarEvent>>();
        WorkContextSnapshot snapshot = Create<WorkContextSnapshot>(
            capturedAtUtc,
            profile,
            manager,
            settings,
            calendar);

        Assert.Equal(capturedAtUtc, snapshot.CapturedAtUtc);
        Assert.Same(profile, snapshot.UserProfile);
        Assert.Same(manager, snapshot.Manager);
        Assert.Same(settings, snapshot.WorkSettings);
        Assert.Same(calendar, snapshot.Calendar);
    }

    [Theory]
    [InlineData(WorkContextFacetStatus.Available, false)]
    [InlineData(WorkContextFacetStatus.Failed, true)]
    public void Snapshot_CopiesCompleteAndPartialCalendarLists(
        WorkContextFacetStatus status,
        bool hasFailure)
    {
        WorkContextCalendarEvent calendarEvent = Create<WorkContextCalendarEvent>(
            DateTimeOffset.Parse("2026-09-14T08:00:00Z"),
            DateTimeOffset.Parse("2026-09-14T08:30:00Z"),
            null,
            null,
            null,
            null,
            null,
            Array.Empty<string>(),
            false,
            false,
            false,
            "busy");
        List<WorkContextCalendarEvent> sourceEvents = [calendarEvent];
        WorkContextFacetFailure? failure = hasFailure
            ? Create<WorkContextFacetFailure>(WorkContextFailureKind.InvalidResponse, null, null)
            : null;
        WorkContextFacetResult<IReadOnlyList<WorkContextCalendarEvent>> calendar =
            Create<WorkContextFacetResult<IReadOnlyList<WorkContextCalendarEvent>>>(
                status,
                sourceEvents,
                failure);

        WorkContextSnapshot snapshot = Create<WorkContextSnapshot>(
            DateTimeOffset.Parse("2026-09-13T12:34:56Z"),
            CreateResult<WorkContextUserProfile>(),
            CreateResult<WorkContextManager>(),
            CreateResult<WorkContextWorkSettings>(),
            calendar);
        sourceEvents.Clear();

        WorkContextCalendarEvent actualEvent = Assert.Single(snapshot.Calendar.Value!);
        Assert.Same(calendarEvent, actualEvent);
        Assert.Throws<NotSupportedException>(() =>
        {
            ((ICollection<WorkContextCalendarEvent>)snapshot.Calendar.Value!).Add(calendarEvent);
        });
    }

    [Fact]
    public void ModelSurface_HasNoAdditionalPublicTypesOrMembers()
    {
        Type[] expectedTypes =
        [
            typeof(IMicrosoft365WorkContextClient),
            typeof(IMicrosoft365WorkContextTokenProvider),
            typeof(Microsoft365WorkContextException),
            typeof(Microsoft365WorkContextOptions),
            typeof(WorkContextCalendarEvent),
            typeof(WorkContextErrorBehavior),
            typeof(WorkContextFacet),
            typeof(WorkContextFacetFailure),
            typeof(WorkContextFacetResult<>),
            typeof(WorkContextFacetStatus),
            typeof(WorkContextFailureKind),
            typeof(WorkContextLocale),
            typeof(WorkContextManager),
            typeof(WorkContextSnapshot),
            typeof(WorkContextUserProfile),
            typeof(WorkContextWorkingHours),
            typeof(WorkContextWorkSettings),
        ];

        Assert.Equal(
            expectedTypes.Select(type => type.FullName).Order(),
            typeof(WorkContextSnapshot).Assembly.GetExportedTypes().Select(type => type.FullName).Order());

        foreach (Type type in expectedTypes.Where(type => type.IsClass && type != typeof(Microsoft365WorkContextException)))
        {
            Assert.Empty(type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly));
            Assert.Empty(type.GetEvents(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly));
            Assert.All(
                type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly),
                method => Assert.True(method.IsSpecialName));
        }
    }

    [Fact]
    public void NullableModelProperties_MatchTheApprovedContract()
    {
        NullabilityInfoContext nullability = new();
        Type[] modelTypes =
        [
            typeof(WorkContextCalendarEvent),
            typeof(WorkContextFacetFailure),
            typeof(WorkContextLocale),
            typeof(WorkContextManager),
            typeof(WorkContextUserProfile),
            typeof(WorkContextWorkSettings),
            typeof(WorkContextWorkingHours),
        ];

        foreach (PropertyInfo property in modelTypes.SelectMany(type => type.GetProperties()))
        {
            if (property.PropertyType == typeof(string) ||
                property.PropertyType == typeof(WorkContextLocale) ||
                property.PropertyType == typeof(WorkContextWorkingHours))
            {
                Assert.Equal(NullabilityState.Nullable, nullability.Create(property).ReadState);
            }
        }
    }

    private static T Create<T>(params object?[] arguments) where T : class =>
        (T)Activator.CreateInstance(
            typeof(T),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: arguments,
            culture: null)!;

    private static WorkContextFacetResult<T> CreateResult<T>() where T : class =>
        Create<WorkContextFacetResult<T>>(WorkContextFacetStatus.Disabled, null, null);

    private static void AssertReadOnlyContract(Type type, params (string Name, Type Type)[] expectedProperties)
    {
        PropertyInfo[] properties = type.GetProperties();

        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors());
        Assert.Equal(
            expectedProperties.OrderBy(property => property.Name),
            properties
                .Select(property => (property.Name, property.PropertyType))
                .OrderBy(property => property.Name));
        Assert.All(properties, property => Assert.Null(property.SetMethod));
    }
}