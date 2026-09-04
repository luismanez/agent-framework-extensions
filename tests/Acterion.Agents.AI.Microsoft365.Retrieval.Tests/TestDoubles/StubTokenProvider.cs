namespace Acterion.Agents.AI.Microsoft365.Retrieval.Tests;

internal sealed class StubTokenProvider(string token = "delegated-token")
    : IMicrosoft365RetrievalTokenProvider
{
    public CancellationToken CancellationToken { get; private set; }

    public int CallCount { get; private set; }

    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        CallCount++;
        CancellationToken = cancellationToken;
        return Task.FromResult(token);
    }
}
