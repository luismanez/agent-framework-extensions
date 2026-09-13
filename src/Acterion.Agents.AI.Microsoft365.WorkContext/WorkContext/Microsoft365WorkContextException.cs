using System.Net;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

/// <summary>Represents a sanitized fail-fast work-context retrieval failure.</summary>
public sealed class Microsoft365WorkContextException : Exception
{
    internal Microsoft365WorkContextException(
        WorkContextFacet? facet,
        WorkContextFailureKind kind,
        HttpStatusCode? statusCode,
        string? requestId)
        : base("Microsoft 365 work-context retrieval failed.")
    {
        if (facet is not null && !Enum.IsDefined(facet.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(facet));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        Facet = facet;
        Kind = kind;
        StatusCode = statusCode;
        RequestId = requestId;
    }

    /// <summary>Gets the owning facet when the failure belongs to one facet.</summary>
    public WorkContextFacet? Facet { get; }

    /// <summary>Gets the normalized failure kind.</summary>
    public WorkContextFailureKind Kind { get; }

    /// <summary>Gets the HTTP status code when Microsoft Graph returned one.</summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>Gets a safe Microsoft Graph request identifier when available.</summary>
    public string? RequestId { get; }
}