using System.Net.Http.Headers;
using System.Net.Http.Json;

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

        if (!this.options.HasEnabledFacet)
        {
            return new WorkContextSnapshot(
                capturedAtUtc,
                Disabled<WorkContextUserProfile>(),
                Disabled<WorkContextManager>(),
                Disabled<WorkContextWorkSettings>(),
                Disabled<IReadOnlyList<WorkContextCalendarEvent>>());
        }

        if (!this.options.EnableUserProfile ||
            this.options.EnableManager ||
            this.options.EnableWorkSettings ||
            this.options.EnableCalendar)
        {
            throw new InvalidOperationException("The requested work-context facets are not available.");
        }

        string accessToken = await this.tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
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

        WorkContextUserProfile profile = new(
            profileResponse.DisplayName,
            profileResponse.GivenName,
            profileResponse.Surname,
            profileResponse.JobTitle,
            profileResponse.Department,
            profileResponse.OfficeLocation,
            profileResponse.PreferredLanguage);

        return new WorkContextSnapshot(
            capturedAtUtc,
            new WorkContextFacetResult<WorkContextUserProfile>(
                WorkContextFacetStatus.Available,
                profile,
                failure: null),
            Disabled<WorkContextManager>(),
            Disabled<WorkContextWorkSettings>(),
            Disabled<IReadOnlyList<WorkContextCalendarEvent>>());
    }

    private static WorkContextFacetResult<T> Disabled<T>() where T : class =>
        new(WorkContextFacetStatus.Disabled, value: null, failure: null);
}