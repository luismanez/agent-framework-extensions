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
        catch (Exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return this.HandleGlobalFailure(
                capturedAtUtc,
                WorkContextFailureKind.TokenAcquisition);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return this.HandleGlobalFailure(capturedAtUtc, WorkContextFailureKind.TokenAcquisition);
        }

        try
        {
            WorkContextSnapshot snapshot = operations.Count > 1
                ? await this.SendBatchAsync(
                    operations,
                    capturedAtUtc,
                    accessToken,
                    cancellationToken).ConfigureAwait(false)
                : await this.SendDirectAsync(
                    operations[0],
                    capturedAtUtc,
                    accessToken,
                    cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            if (this.options.ErrorBehavior == WorkContextErrorBehavior.FailFast)
            {
                ThrowFirstFacetFailure(snapshot);
            }

            return snapshot;
        }
        catch (HttpRequestException exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return this.HandleGlobalFailure(
                capturedAtUtc,
                WorkContextFailureKind.Transport,
                exception.StatusCode);
        }
        catch (IOException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return this.HandleGlobalFailure(capturedAtUtc, WorkContextFailureKind.Transport);
        }
        catch (OperationCanceledException exception) when (
            !cancellationToken.IsCancellationRequested && exception.InnerException is TimeoutException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return this.HandleGlobalFailure(capturedAtUtc, WorkContextFailureKind.Transport);
        }
    }

    private async Task<WorkContextSnapshot> SendDirectAsync(
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

        cancellationToken.ThrowIfCancellationRequested();
        JsonElement body = default;
        if (response.IsSuccessStatusCode)
        {
            try
            {
                body = await response.Content
                    .ReadFromJsonAsync<JsonElement>(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // The classifier converts the undefined payload into a sanitized InvalidResponse failure.
            }
            catch (IOException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WorkContextFacetFailure failure = new(
                    WorkContextFailureKind.Transport,
                    response.StatusCode,
                    MicrosoftGraphOperationOutcomeClassifier.SelectRequestId(
                        GetHeaderValue(response, "request-id"),
                        GetHeaderValue(response, "client-request-id")));
                return Microsoft365WorkContextBestEffortFacetReducer.Reduce(
                    capturedAtUtc,
                    this.options,
                    [MicrosoftGraphOperationOutcome.Failed(operation, failure)]);
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

        cancellationToken.ThrowIfCancellationRequested();
        if (!response.IsSuccessStatusCode)
        {
            return this.HandleGlobalHttpFailure(capturedAtUtc, response);
        }

        IReadOnlyList<MicrosoftGraphBatchSubresponse> responses;
        try
        {
            responses = await MicrosoftGraphBatchResponseParser
                .ParseAsync(response.Content, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return this.HandleGlobalResponseFailure(
                capturedAtUtc,
                WorkContextFailureKind.InvalidResponse,
                response);
        }
        catch (IOException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return this.HandleGlobalResponseFailure(
                capturedAtUtc,
                WorkContextFailureKind.Transport,
                response);
        }

        IReadOnlyList<MicrosoftGraphCorrelatedResponse> correlated =
            MicrosoftGraphBatchResponseCorrelator.Correlate(operations, responses);

        IReadOnlyList<MicrosoftGraphOperationOutcome> outcomes = correlated
            .Select(MicrosoftGraphOperationOutcomeClassifier.Classify)
            .ToArray();

        return Microsoft365WorkContextBestEffortFacetReducer.Reduce(
            capturedAtUtc,
            this.options,
            outcomes);
    }

    private WorkContextSnapshot HandleGlobalHttpFailure(
        DateTimeOffset capturedAtUtc,
        HttpResponseMessage response)
    {
        WorkContextFailureKind failureKind =
            MicrosoftGraphOperationOutcomeClassifier.ClassifyFailureKind(response.StatusCode);

        return this.HandleGlobalResponseFailure(capturedAtUtc, failureKind, response);
    }

    private WorkContextSnapshot HandleGlobalResponseFailure(
        DateTimeOffset capturedAtUtc,
        WorkContextFailureKind failureKind,
        HttpResponseMessage response)
    {
        string? requestId = MicrosoftGraphOperationOutcomeClassifier.SelectRequestId(
            GetHeaderValue(response, "request-id"),
            GetHeaderValue(response, "client-request-id"));

        return this.HandleGlobalFailure(
            capturedAtUtc,
            failureKind,
            response.StatusCode,
            requestId);
    }

    private WorkContextSnapshot HandleGlobalFailure(
        DateTimeOffset capturedAtUtc,
        WorkContextFailureKind failureKind,
        HttpStatusCode? statusCode = null,
        string? requestId = null) =>
        this.options.ErrorBehavior == WorkContextErrorBehavior.BestEffort
            ? Microsoft365WorkContextGlobalFailureReducer.Reduce(
                capturedAtUtc, this.options, failureKind, statusCode, requestId)
            : throw new Microsoft365WorkContextException(
                facet: null, failureKind, statusCode, requestId);

    private static void ThrowFirstFacetFailure(WorkContextSnapshot snapshot)
    {
        if (snapshot.UserProfile.Failure is { } profile)
        {
            throw ToException(WorkContextFacet.UserProfile, profile);
        }

        if (snapshot.Manager.Failure is { } manager)
        {
            throw ToException(WorkContextFacet.Manager, manager);
        }

        if (snapshot.WorkSettings.Failure is { } workSettings)
        {
            throw ToException(WorkContextFacet.WorkSettings, workSettings);
        }

        if (snapshot.Calendar.Failure is { } calendar)
        {
            throw ToException(WorkContextFacet.Calendar, calendar);
        }
    }

    private static Microsoft365WorkContextException ToException(
        WorkContextFacet facet,
        WorkContextFacetFailure failure) =>
        new(facet, failure.Kind, failure.StatusCode, failure.RequestId);

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
