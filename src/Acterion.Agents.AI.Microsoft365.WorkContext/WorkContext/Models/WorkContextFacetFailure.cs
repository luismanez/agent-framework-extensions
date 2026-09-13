using System.Net;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Contains sanitized information about a work-context facet failure.</summary>
public sealed class WorkContextFacetFailure
{
    internal WorkContextFacetFailure(
        WorkContextFailureKind kind,
        HttpStatusCode? statusCode,
        string? requestId)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        Kind = kind;
        StatusCode = statusCode;
        RequestId = requestId;
    }

    /// <summary>Gets the normalized failure kind.</summary>
    public WorkContextFailureKind Kind { get; }

    /// <summary>Gets the HTTP status code when Microsoft Graph returned one.</summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>Gets a safe Microsoft Graph request identifier when available.</summary>
    public string? RequestId { get; }
}