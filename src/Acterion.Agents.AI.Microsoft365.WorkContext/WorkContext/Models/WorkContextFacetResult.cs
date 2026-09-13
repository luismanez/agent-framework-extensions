namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Represents the outcome of retrieving one work-context facet.</summary>
/// <typeparam name="T">The facet value type.</typeparam>
public sealed class WorkContextFacetResult<T> where T : class
{
    internal WorkContextFacetResult(
        WorkContextFacetStatus status,
        T? value,
        WorkContextFacetFailure? failure)
    {
        bool isValid = status switch
        {
            WorkContextFacetStatus.Disabled => value is null && failure is null,
            WorkContextFacetStatus.Available => value is not null && failure is null,
            WorkContextFacetStatus.Unavailable => value is null && failure is null,
            WorkContextFacetStatus.Failed => failure is not null,
            _ => false,
        };

        if (!isValid)
        {
            throw new ArgumentException("The facet result state is invalid.", nameof(status));
        }

        Status = status;
        Value = value;
        Failure = failure;
    }

    /// <summary>Gets the facet retrieval status.</summary>
    public WorkContextFacetStatus Status { get; }

    /// <summary>Gets the complete or partial facet value, when available.</summary>
    public T? Value { get; }

    /// <summary>Gets sanitized failure information when retrieval failed.</summary>
    public WorkContextFacetFailure? Failure { get; }
}