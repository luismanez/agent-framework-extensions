using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal sealed class Microsoft365WorkContextClient : IMicrosoft365WorkContextClient
{
    private readonly HttpClient httpClient;
    private readonly Microsoft365WorkContextOptionsSnapshot options;
    private readonly TimeProvider timeProvider;
    private readonly IMicrosoft365WorkContextTokenProvider tokenProvider;

    internal Microsoft365WorkContextClient(
        HttpClient httpClient,
        IMicrosoft365WorkContextTokenProvider tokenProvider,
        Microsoft365WorkContextOptions options,
        TimeProvider timeProvider)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        this.options = Microsoft365WorkContextOptionsValidator.ValidateAndSnapshot(options);
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<WorkContextSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTimeOffset capturedAtUtc = this.timeProvider.GetUtcNow();
        IReadOnlyList<MicrosoftGraphOperation> operations =
            MicrosoftGraphOperations.Select(this.options, capturedAtUtc);

        if (operations.Count == 0)
        {
            return new WorkContextSnapshot(
                capturedAtUtc,
                Disabled<WorkContextUserProfile>(),
                Disabled<WorkContextManager>(),
                Disabled<WorkContextWorkSettings>(),
                Disabled<IReadOnlyList<WorkContextCalendarEvent>>());
        }

        string accessToken;
        try
        {
            accessToken = await this.tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception) when (this.options.ErrorBehavior == WorkContextErrorBehavior.BestEffort)
        {
            return Microsoft365WorkContextGlobalFailureReducer.Reduce(
                capturedAtUtc,
                this.options,
                WorkContextFailureKind.TokenAcquisition);
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            if (this.options.ErrorBehavior == WorkContextErrorBehavior.BestEffort)
            {
                return Microsoft365WorkContextGlobalFailureReducer.Reduce(
                    capturedAtUtc,
                    this.options,
                    WorkContextFailureKind.TokenAcquisition);
            }

            throw new Microsoft365WorkContextException(
                facet: null,
                WorkContextFailureKind.TokenAcquisition,
                statusCode: null,
                requestId: null);
        }

        if (this.options.ErrorBehavior != WorkContextErrorBehavior.BestEffort &&
            operations.Count == 1 &&
            operations[0].Id is not ("profile" or "manager"))
        {
            throw new InvalidOperationException("The requested work-context facets are not available.");
        }

        try
        {
            if (operations.Count > 1)
            {
                return await this.SendBatchAsync(
                    operations,
                    capturedAtUtc,
                    accessToken,
                    cancellationToken).ConfigureAwait(false);
            }

            if (this.options.ErrorBehavior == WorkContextErrorBehavior.BestEffort)
            {
                return await this.SendBestEffortDirectAsync(
                    operations[0],
                    capturedAtUtc,
                    accessToken,
                    cancellationToken).ConfigureAwait(false);
            }

            return operations[0].Id == "profile"
                ? await this.GetProfileSnapshotAsync(capturedAtUtc, accessToken, cancellationToken).ConfigureAwait(false)
                : await this.GetManagerSnapshotAsync(capturedAtUtc, accessToken, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException exception)
            when (this.options.ErrorBehavior == WorkContextErrorBehavior.BestEffort)
        {
            return Microsoft365WorkContextGlobalFailureReducer.Reduce(
                capturedAtUtc,
                this.options,
                WorkContextFailureKind.Transport,
                exception.StatusCode);
        }
    }

    private async Task<WorkContextSnapshot> SendBestEffortDirectAsync(
        MicrosoftGraphOperation operation,
        DateTimeOffset capturedAtUtc,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, operation.DirectUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await this.httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        JsonElement body = default;
        if (response.IsSuccessStatusCode)
        {
            try
            {
                body = await response.Content
                    .ReadFromJsonAsync<JsonElement>(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (JsonException)
            {
                // The classifier converts the undefined payload into a sanitized InvalidResponse failure.
            }
        }

        MicrosoftGraphOperationOutcome outcome = MicrosoftGraphOperationOutcomeClassifier.Classify(
            operation,
            (int)response.StatusCode,
            GetOutcomeHeaders(response),
            body);

        return Microsoft365WorkContextBestEffortFacetReducer.Reduce(
            capturedAtUtc,
            this.options,
            [outcome]);
    }

    private async Task<WorkContextSnapshot> SendBatchAsync(
        IReadOnlyList<MicrosoftGraphOperation> operations,
        DateTimeOffset capturedAtUtc,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, "v1.0/$batch")
        {
            Content = MicrosoftGraphBatchRequestSerializer.CreateContent(operations),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await this.httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode &&
            this.options.ErrorBehavior == WorkContextErrorBehavior.BestEffort)
        {
            return this.ReduceGlobalHttpFailure(capturedAtUtc, response);
        }

        response.EnsureSuccessStatusCode();

        IReadOnlyList<MicrosoftGraphBatchSubresponse> responses;
        try
        {
            responses = await MicrosoftGraphBatchResponseParser
                .ParseAsync(response.Content, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception)
            when (this.options.ErrorBehavior == WorkContextErrorBehavior.BestEffort &&
                exception is JsonException or InvalidDataException)
        {
            return this.ReduceGlobalFailure(
                capturedAtUtc,
                WorkContextFailureKind.InvalidResponse,
                response);
        }

        IReadOnlyList<MicrosoftGraphCorrelatedResponse> correlated =
            MicrosoftGraphBatchResponseCorrelator.Correlate(operations, responses);

        if (this.options.ErrorBehavior == WorkContextErrorBehavior.BestEffort)
        {
            IReadOnlyList<MicrosoftGraphOperationOutcome> outcomes = correlated
                .Select(MicrosoftGraphOperationOutcomeClassifier.Classify)
                .ToArray();

            return Microsoft365WorkContextBestEffortFacetReducer.Reduce(
                capturedAtUtc,
                this.options,
                outcomes);
        }

        if (correlated.Count == 2 &&
            correlated[0].Operation.Id == "profile" &&
            correlated[1].Operation.Id == "manager" &&
            correlated.All(item => item.IsValid && item.Response?.Status == 200))
        {
            MicrosoftGraphUserProfileResponse profileResponse =
                correlated[0].Response!.Body.Deserialize<MicrosoftGraphUserProfileResponse>()
                ?? throw new InvalidDataException("The Profile batch response body is invalid.");
            MicrosoftGraphManagerResponse managerResponse =
                correlated[1].Response!.Body.Deserialize<MicrosoftGraphManagerResponse>()
                ?? throw new InvalidDataException("The Manager batch response body is invalid.");

            return new WorkContextSnapshot(
                capturedAtUtc,
                new WorkContextFacetResult<WorkContextUserProfile>(
                    WorkContextFacetStatus.Available,
                    MapProfile(profileResponse),
                    failure: null),
                new WorkContextFacetResult<WorkContextManager>(
                    WorkContextFacetStatus.Available,
                    MapManager(managerResponse),
                    failure: null),
                Disabled<WorkContextWorkSettings>(),
                Disabled<IReadOnlyList<WorkContextCalendarEvent>>());
        }

        throw new InvalidOperationException("Batch response parsing is not available.");
    }

    private async Task<WorkContextSnapshot> GetProfileSnapshotAsync(
        DateTimeOffset capturedAtUtc,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, MicrosoftGraphOperations.Profile);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await this.httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode &&
            this.options.ErrorBehavior == WorkContextErrorBehavior.BestEffort)
        {
            return this.ReduceGlobalHttpFailure(capturedAtUtc, response);
        }

        response.EnsureSuccessStatusCode();

        MicrosoftGraphUserProfileResponse profileResponse = await response.Content
            .ReadFromJsonAsync<MicrosoftGraphUserProfileResponse>(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Microsoft Graph returned an empty Profile response.");

        return new WorkContextSnapshot(
            capturedAtUtc,
            new WorkContextFacetResult<WorkContextUserProfile>(
                WorkContextFacetStatus.Available,
                MapProfile(profileResponse),
                failure: null),
            Disabled<WorkContextManager>(),
            Disabled<WorkContextWorkSettings>(),
            Disabled<IReadOnlyList<WorkContextCalendarEvent>>());
    }

    private async Task<WorkContextSnapshot> GetManagerSnapshotAsync(
        DateTimeOffset capturedAtUtc,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, MicrosoftGraphOperations.Manager);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using HttpResponseMessage response = await this.httpClient
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new WorkContextSnapshot(
                capturedAtUtc,
                Disabled<WorkContextUserProfile>(),
                new WorkContextFacetResult<WorkContextManager>(
                    WorkContextFacetStatus.Unavailable,
                    value: null,
                    failure: null),
                Disabled<WorkContextWorkSettings>(),
                Disabled<IReadOnlyList<WorkContextCalendarEvent>>());
        }

        if (!response.IsSuccessStatusCode &&
            this.options.ErrorBehavior == WorkContextErrorBehavior.BestEffort)
        {
            return this.ReduceGlobalHttpFailure(capturedAtUtc, response);
        }

        response.EnsureSuccessStatusCode();

        MicrosoftGraphManagerResponse managerResponse = await response.Content
            .ReadFromJsonAsync<MicrosoftGraphManagerResponse>(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Microsoft Graph returned an empty Manager response.");

        return new WorkContextSnapshot(
            capturedAtUtc,
            Disabled<WorkContextUserProfile>(),
            new WorkContextFacetResult<WorkContextManager>(
                WorkContextFacetStatus.Available,
                MapManager(managerResponse),
                failure: null),
            Disabled<WorkContextWorkSettings>(),
            Disabled<IReadOnlyList<WorkContextCalendarEvent>>());
    }

    private static WorkContextUserProfile MapProfile(MicrosoftGraphUserProfileResponse response) =>
        new(
            response.DisplayName,
            response.GivenName,
            response.Surname,
            response.JobTitle,
            response.Department,
            response.OfficeLocation,
            response.PreferredLanguage);

    private static WorkContextManager MapManager(MicrosoftGraphManagerResponse response) =>
        new(
            response.DisplayName,
            response.JobTitle,
            response.Department,
            response.OfficeLocation);

    private WorkContextSnapshot ReduceGlobalHttpFailure(
        DateTimeOffset capturedAtUtc,
        HttpResponseMessage response)
    {
        WorkContextFailureKind failureKind =
            MicrosoftGraphOperationOutcomeClassifier.ClassifyFailureKind(response.StatusCode);

        return this.ReduceGlobalFailure(capturedAtUtc, failureKind, response);
    }

    private WorkContextSnapshot ReduceGlobalFailure(
        DateTimeOffset capturedAtUtc,
        WorkContextFailureKind failureKind,
        HttpResponseMessage response)
    {
        string? requestId = MicrosoftGraphOperationOutcomeClassifier.SelectRequestId(
            GetHeaderValue(response, "request-id"),
            GetHeaderValue(response, "client-request-id"));

        return Microsoft365WorkContextGlobalFailureReducer.Reduce(
            capturedAtUtc,
            this.options,
            failureKind,
            response.StatusCode,
            requestId);
    }

    private static string? GetHeaderValue(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out IEnumerable<string>? values)
            ? values.FirstOrDefault()
            : null;

    private static IReadOnlyDictionary<string, string>? GetOutcomeHeaders(HttpResponseMessage response)
    {
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        AddHeaderIfPresent(headers, response, "request-id");
        AddHeaderIfPresent(headers, response, "client-request-id");
        return headers.Count == 0 ? null : headers;
    }

    private static void AddHeaderIfPresent(
        IDictionary<string, string> headers,
        HttpResponseMessage response,
        string name)
    {
        string? value = GetHeaderValue(response, name);
        if (value is not null)
        {
            headers.Add(name, value);
        }
    }

    private static WorkContextFacetResult<T> Disabled<T>() where T : class =>
        new(WorkContextFacetStatus.Disabled, value: null, failure: null);
}
