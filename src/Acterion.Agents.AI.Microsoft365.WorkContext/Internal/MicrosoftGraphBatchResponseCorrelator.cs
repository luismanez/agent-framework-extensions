namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal static class MicrosoftGraphBatchResponseCorrelator
{
    internal static IReadOnlyList<MicrosoftGraphCorrelatedResponse> Correlate(
        IReadOnlyList<MicrosoftGraphOperation> operations,
        IReadOnlyList<MicrosoftGraphBatchSubresponse> responses)
    {
        ArgumentNullException.ThrowIfNull(operations);
        ArgumentNullException.ThrowIfNull(responses);

        MicrosoftGraphCorrelatedResponse[] correlated = operations
            .Select(operation =>
            {
                MicrosoftGraphBatchSubresponse[] matches = responses
                    .Where(response => string.Equals(response.Id, operation.Id, StringComparison.Ordinal))
                    .Take(2)
                    .ToArray();

                return matches.Length == 1
                    ? new MicrosoftGraphCorrelatedResponse(operation, matches[0], IsValid: true)
                    : new MicrosoftGraphCorrelatedResponse(operation, Response: null, IsValid: false);
            })
            .ToArray();

        return Array.AsReadOnly(correlated);
    }
}

internal sealed record MicrosoftGraphCorrelatedResponse(
    MicrosoftGraphOperation Operation,
    MicrosoftGraphBatchSubresponse? Response,
    bool IsValid);