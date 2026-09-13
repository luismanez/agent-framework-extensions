using System.Net;
using System.Reflection;
using Xunit;

namespace Acterion.Agents.AI.Microsoft365.WorkContext.Tests.PublicContract;

public sealed class WorkContextResultPrimitiveContractTests
{
    [Fact]
    public void ResultTypes_AreSealedReadOnlyAndNotPubliclyConstructible()
    {
        AssertReadOnlyContract(
            typeof(WorkContextFacetResult<SampleValue>),
            ("Failure", typeof(WorkContextFacetFailure)),
            ("Status", typeof(WorkContextFacetStatus)),
            ("Value", typeof(SampleValue)));
        AssertReadOnlyContract(
            typeof(WorkContextFacetFailure),
            ("Kind", typeof(WorkContextFailureKind)),
            ("RequestId", typeof(string)),
            ("StatusCode", typeof(HttpStatusCode?)));
        AssertReadOnlyContract(
            typeof(Microsoft365WorkContextException),
            ("Facet", typeof(WorkContextFacet?)),
            ("Kind", typeof(WorkContextFailureKind)),
            ("RequestId", typeof(string)),
            ("StatusCode", typeof(HttpStatusCode?)));
    }

    [Theory]
    [InlineData(WorkContextFacetStatus.Disabled, false, false)]
    [InlineData(WorkContextFacetStatus.Available, true, false)]
    [InlineData(WorkContextFacetStatus.Unavailable, false, false)]
    [InlineData(WorkContextFacetStatus.Failed, false, true)]
    [InlineData(WorkContextFacetStatus.Failed, true, true)]
    public void FacetResult_AcceptsValidStates(
        WorkContextFacetStatus status,
        bool hasValue,
        bool hasFailure)
    {
        SampleValue? value = hasValue ? new SampleValue() : null;
        WorkContextFacetFailure? failure = hasFailure ? CreateFailure() : null;

        WorkContextFacetResult<SampleValue> result = CreateResult(status, value, failure);

        Assert.Equal(status, result.Status);
        Assert.Same(value, result.Value);
        Assert.Same(failure, result.Failure);
    }

    [Theory]
    [InlineData(WorkContextFacetStatus.Disabled, true, false)]
    [InlineData(WorkContextFacetStatus.Disabled, false, true)]
    [InlineData(WorkContextFacetStatus.Available, false, false)]
    [InlineData(WorkContextFacetStatus.Available, true, true)]
    [InlineData(WorkContextFacetStatus.Unavailable, true, false)]
    [InlineData(WorkContextFacetStatus.Unavailable, false, true)]
    [InlineData(WorkContextFacetStatus.Failed, false, false)]
    public void FacetResult_RejectsInvalidStates(
        WorkContextFacetStatus status,
        bool hasValue,
        bool hasFailure)
    {
        SampleValue? value = hasValue ? new SampleValue() : null;
        WorkContextFacetFailure? failure = hasFailure ? CreateFailure() : null;

        TargetInvocationException exception = Assert.Throws<TargetInvocationException>(
            () => CreateResult(status, value, failure));

        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Fact]
    public void FailureAndException_ExposeOnlySanitizedValues()
    {
        WorkContextFacetFailure failure = CreateFailure();
        Microsoft365WorkContextException exception = CreateException();

        Assert.Equal(WorkContextFailureKind.Authorization, failure.Kind);
        Assert.Equal(HttpStatusCode.Forbidden, failure.StatusCode);
        Assert.Equal("safe-request-id", failure.RequestId);
        Assert.Equal(WorkContextFacet.Manager, exception.Facet);
        Assert.Equal(WorkContextFailureKind.Authorization, exception.Kind);
        Assert.Equal(HttpStatusCode.Forbidden, exception.StatusCode);
        Assert.Equal("safe-request-id", exception.RequestId);
        Assert.Null(exception.InnerException);
        Assert.DoesNotContain("safe-request-id", exception.Message, StringComparison.Ordinal);
    }

    private static WorkContextFacetResult<SampleValue> CreateResult(
        WorkContextFacetStatus status,
        SampleValue? value,
        WorkContextFacetFailure? failure) =>
        (WorkContextFacetResult<SampleValue>)Activator.CreateInstance(
            typeof(WorkContextFacetResult<SampleValue>),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [status, value, failure],
            culture: null)!;

    private static WorkContextFacetFailure CreateFailure() =>
        (WorkContextFacetFailure)Activator.CreateInstance(
            typeof(WorkContextFacetFailure),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [WorkContextFailureKind.Authorization, HttpStatusCode.Forbidden, "safe-request-id"],
            culture: null)!;

    private static Microsoft365WorkContextException CreateException() =>
        (Microsoft365WorkContextException)Activator.CreateInstance(
            typeof(Microsoft365WorkContextException),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [WorkContextFacet.Manager, WorkContextFailureKind.Authorization, HttpStatusCode.Forbidden, "safe-request-id"],
            culture: null)!;

    private static void AssertReadOnlyContract(Type type, params (string Name, Type Type)[] expectedProperties)
    {
        PropertyInfo[] properties = type.GetProperties(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        Assert.True(type.IsSealed);
        Assert.Empty(type.GetConstructors());
        Assert.Equal(
            expectedProperties.OrderBy(property => property.Name),
            properties
                .Select(property => (property.Name, property.PropertyType))
                .OrderBy(property => property.Name));
        Assert.All(properties, property => Assert.Null(property.SetMethod));
    }

    private sealed class SampleValue;
}