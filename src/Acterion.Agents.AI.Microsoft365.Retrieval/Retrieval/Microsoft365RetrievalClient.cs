using System.Net;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acterion.Agents.AI.Microsoft365.Retrieval;

/// <summary>
/// Retrieves permission-trimmed SharePoint content through the Microsoft 365 Copilot Retrieval API.
/// </summary>
public sealed class Microsoft365RetrievalClient : IMicrosoft365RetrievalClient
{
    private const int MaximumQueryLength = 1_500;
    private static readonly Uri RetrievalEndpoint = new(
        "https://graph.microsoft.com/v1.0/copilot/retrieval");
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;
    private readonly ILogger<Microsoft365RetrievalClient>? logger;
    private readonly Microsoft365RetrievalOptions options;
    private readonly IMicrosoft365RetrievalTokenProvider tokenProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Microsoft365RetrievalClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to call Microsoft Graph.</param>
    /// <param name="tokenProvider">The host-provided delegated token source.</param>
    /// <param name="options">The retrieval request options.</param>
    /// <param name="logger">The optional logger used for safe operational diagnostics.</param>
    /// <exception cref="OptionsValidationException">The retrieval options are invalid.</exception>
    public Microsoft365RetrievalClient(
        HttpClient httpClient,
        IMicrosoft365RetrievalTokenProvider tokenProvider,
        Microsoft365RetrievalOptions options,
        ILogger<Microsoft365RetrievalClient>? logger = null)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        this.options = Microsoft365RetrievalOptionsValidator.ValidateAndSnapshot(
            options ?? throw new ArgumentNullException(nameof(options)));
        this.logger = logger;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Microsoft365RetrievalClient"/> class from validated options.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to call Microsoft Graph.</param>
    /// <param name="tokenProvider">The host-provided delegated token source.</param>
    /// <param name="options">The retrieval request options, validated during construction.</param>
    /// <param name="logger">The logger used for safe operational diagnostics.</param>
    /// <exception cref="OptionsValidationException">The retrieval options are invalid.</exception>
    public Microsoft365RetrievalClient(
        HttpClient httpClient,
        IMicrosoft365RetrievalTokenProvider tokenProvider,
        IOptions<Microsoft365RetrievalOptions> options,
        ILogger<Microsoft365RetrievalClient> logger)
        : this(httpClient, tokenProvider, options?.Value ?? throw new ArgumentNullException(nameof(options)), logger)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Microsoft365RetrievalHit>> RetrieveAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        Stopwatch stopwatch = Stopwatch.StartNew();
        RetrievalLogEvents.LogStarted(
            this.logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<Microsoft365RetrievalClient>.Instance,
            this.options.MaximumNumberOfResults,
            this.options.FilterExpression is not null);

        string accessToken;
        try
        {
            accessToken = await this.tokenProvider
                .GetAccessTokenAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            RetrievalLogEvents.LogFailed(this.GetLogger(), stopwatch.ElapsedMilliseconds, null);
            throw;
        }
        RetrievalApiRequest payload = new()
        {
            QueryString = query,
            FilterExpression = this.options.FilterExpression,
            ResourceMetadata = this.options.ResourceMetadata,
            MaximumNumberOfResults = this.options.MaximumNumberOfResults,
        };
        byte[] requestBytes = JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions);

        using HttpRequestMessage request = new(HttpMethod.Post, RetrievalEndpoint)
        {
            Content = new ByteArrayContent(requestBytes),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        {
            CharSet = "utf-8",
        };

        HttpResponseMessage response;
        try
        {
            response = await this.httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
        {
            RetrievalLogEvents.LogFailed(this.GetLogger(), stopwatch.ElapsedMilliseconds, (int?)exception.StatusCode);
            throw new Microsoft365RetrievalException(
                "The retrieval request could not reach Microsoft Graph.",
                exception.StatusCode,
                innerException: exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    RetrievalLogEvents.LogThrottled(this.GetLogger(), stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    RetrievalLogEvents.LogFailed(this.GetLogger(), stopwatch.ElapsedMilliseconds, (int)response.StatusCode);
                }

                throw new Microsoft365RetrievalException(
                    GetFailureMessage(response.StatusCode),
                    response.StatusCode,
                    GetRequestId(response));
            }

            await using Stream responseStream = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            try
            {
                RetrievalApiResponse? payloadResponse = await JsonSerializer
                    .DeserializeAsync<RetrievalApiResponse>(
                        responseStream,
                        SerializerOptions,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (payloadResponse?.RetrievalHits is null)
                {
                    throw new JsonException("The response did not contain retrievalHits.");
                }

                IReadOnlyList<Microsoft365RetrievalHit> hits = payloadResponse.RetrievalHits.Select(MapHit).ToArray();
                RetrievalLogEvents.LogCompleted(this.GetLogger(), stopwatch.ElapsedMilliseconds, hits.Count);
                return hits;
            }
            catch (JsonException exception)
            {
                RetrievalLogEvents.LogFailed(this.GetLogger(), stopwatch.ElapsedMilliseconds, (int)response.StatusCode);
                throw new Microsoft365RetrievalException(
                    "Microsoft Graph returned an invalid retrieval response.",
                    response.StatusCode,
                    innerException: exception);
            }
        }
    }

    private static void ValidateQuery(string query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("The retrieval query cannot be empty or whitespace.", nameof(query));
        }

        if (query.Length > MaximumQueryLength)
        {
            throw new ArgumentException(
                $"The retrieval query cannot exceed {MaximumQueryLength} characters.",
                nameof(query));
        }
    }

    private static Microsoft365RetrievalHit MapHit(RetrievalApiHit hit)
    {
        if (string.IsNullOrEmpty(hit.WebUrl))
        {
            throw new JsonException("A retrieval hit did not contain webUrl.");
        }

        IReadOnlyList<Microsoft365RetrievalExtract> extracts = (hit.Extracts ?? [])
            .Select(extract => new Microsoft365RetrievalExtract(
                extract.Text ?? throw new JsonException("A retrieval extract did not contain text."),
                extract.RelevanceScore))
            .ToArray();

        try
        {
            return new Microsoft365RetrievalHit(
                hit.WebUrl,
                extracts,
                hit.ResourceType,
                hit.ResourceMetadata ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal),
                MapSensitivityLabel(hit.SensitivityLabel));
        }
        catch (ArgumentException exception)
        {
            throw new JsonException("A retrieval hit contained invalid metadata.", exception);
        }
    }

    private static Microsoft365RetrievalSensitivityLabel? MapSensitivityLabel(
        RetrievalApiSensitivityLabel? sensitivityLabel) =>
        sensitivityLabel is null
            ? null
            : new Microsoft365RetrievalSensitivityLabel(
                sensitivityLabel.SensitivityLabelId,
                sensitivityLabel.DisplayName,
                sensitivityLabel.ToolTip,
                sensitivityLabel.Priority,
                sensitivityLabel.Color);

    private static string GetFailureMessage(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => "The retrieval request was rejected.",
        HttpStatusCode.Unauthorized => "The delegated access token was rejected.",
        HttpStatusCode.Forbidden => "The delegated user is not authorized to retrieve this content.",
        HttpStatusCode.TooManyRequests => "Microsoft Graph throttled the retrieval request.",
        >= HttpStatusCode.InternalServerError => "Microsoft Graph is temporarily unavailable.",
        _ => "Microsoft Graph returned an unsuccessful retrieval response.",
    };

    private static string? GetRequestId(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("request-id", out IEnumerable<string>? requestIds)
            ? requestIds.FirstOrDefault()
            : response.Headers.TryGetValues("client-request-id", out IEnumerable<string>? clientRequestIds)
                ? clientRequestIds.FirstOrDefault()
                : null;
    }

    private ILogger GetLogger() => this.logger
        ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<Microsoft365RetrievalClient>.Instance;
}
