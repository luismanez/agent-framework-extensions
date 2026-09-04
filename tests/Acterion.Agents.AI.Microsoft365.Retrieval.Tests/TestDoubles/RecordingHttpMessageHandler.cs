using System.Net;
using System.Net.Http.Headers;

namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

internal sealed class RecordingHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
{
    public AuthenticationHeaderValue? Authorization { get; private set; }

    public string? Charset { get; private set; }

    public string? Content { get; private set; }

    public string? ContentType { get; private set; }

    public HttpMethod? Method { get; private set; }

    public int RequestCount { get; private set; }

    public Uri? RequestUri { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;
        Method = request.Method;
        RequestUri = request.RequestUri;
        Authorization = request.Headers.Authorization;
        ContentType = request.Content?.Headers.ContentType?.MediaType;
        Charset = request.Content?.Headers.ContentType?.CharSet;
        Content = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        response.StatusCode = response.StatusCode == default
            ? HttpStatusCode.OK
            : response.StatusCode;
        return response;
    }
}
