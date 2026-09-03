using System.Net;

namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Represents a failure while retrieving Microsoft 365 content.
/// </summary>
public sealed class Microsoft365RetrievalException : Exception
{
    /// <summary>
    /// Initializes a new retrieval exception with inspectable HTTP diagnostics.
    /// </summary>
    /// <param name="message">A safe description of the failure.</param>
    /// <param name="statusCode">The HTTP status returned by Microsoft Graph, when available.</param>
    /// <param name="requestId">The Graph request or correlation identifier, when available.</param>
    /// <param name="innerException">The exception that caused this failure, when available.</param>
    public Microsoft365RetrievalException(
        string message,
        HttpStatusCode? statusCode = null,
        string? requestId = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        RequestId = requestId;
    }

    /// <summary>
    /// Gets the HTTP status returned by Microsoft Graph, when available.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// Gets the Graph request or correlation identifier, when available.
    /// </summary>
    public string? RequestId { get; }
}