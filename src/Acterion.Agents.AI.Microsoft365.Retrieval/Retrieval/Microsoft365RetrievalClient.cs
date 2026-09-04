using System.Net.Http.Headers;
using System.Text.Json;

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
    private readonly Microsoft365RetrievalOptions options;
    private readonly IMicrosoft365RetrievalTokenProvider tokenProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Microsoft365RetrievalClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to call Microsoft Graph.</param>
    /// <param name="tokenProvider">The host-provided delegated token source.</param>
    /// <param name="options">The retrieval request options.</param>
    public Microsoft365RetrievalClient(
        HttpClient httpClient,
        IMicrosoft365RetrievalTokenProvider tokenProvider,
        Microsoft365RetrievalOptions options)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Microsoft365RetrievalHit>> RetrieveAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);

        string accessToken = await this.tokenProvider
            .GetAccessTokenAsync(cancellationToken)
            .ConfigureAwait(false);
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

        using HttpResponseMessage response = await this.httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

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

            return payloadResponse.RetrievalHits.Select(MapHit).ToArray();
        }
        catch (JsonException exception)
        {
            throw new Microsoft365RetrievalException(
                "Microsoft Graph returned an invalid retrieval response.",
                response.StatusCode,
                innerException: exception);
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
                hit.ResourceMetadata ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal));
        }
        catch (ArgumentException exception)
        {
            throw new JsonException("A retrieval hit contained invalid metadata.", exception);
        }
    }
}
