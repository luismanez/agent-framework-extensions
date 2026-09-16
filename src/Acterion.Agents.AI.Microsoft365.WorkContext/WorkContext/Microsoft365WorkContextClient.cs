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

        if (operations.Count == 1 && operations[0].Id is not ("profile" or "manager"))
        {
            throw new InvalidOperationException("The requested work-context facets are not available.");
        }

        string accessToken = await this.tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        if (operations.Count > 1)
        {
            return await this.SendBatchAsync(
                operations,
                capturedAtUtc,
                accessToken,
                cancellationToken).ConfigureAwait(false);
        }

        return operations[0].Id == "profile"
            ? await this.GetProfileSnapshotAsync(capturedAtUtc, accessToken, cancellationToken).ConfigureAwait(false)
            : await this.GetManagerSnapshotAsync(capturedAtUtc, accessToken, cancellationToken).ConfigureAwait(false);
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
        response.EnsureSuccessStatusCode();

        IReadOnlyList<MicrosoftGraphBatchSubresponse> responses =
            await MicrosoftGraphBatchResponseParser.ParseAsync(response.Content, cancellationToken)
                .ConfigureAwait(false);
        IReadOnlyList<MicrosoftGraphCorrelatedResponse> correlated =
            MicrosoftGraphBatchResponseCorrelator.Correlate(operations, responses);

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

    private static WorkContextFacetResult<T> Disabled<T>() where T : class =>
        new(WorkContextFacetStatus.Disabled, value: null, failure: null);
}