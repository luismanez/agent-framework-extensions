using System.Net.Http.Json;

namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal static class MicrosoftGraphBatchRequestSerializer
{
    internal static JsonContent CreateContent(IReadOnlyList<MicrosoftGraphOperation> operations)
    {
        ArgumentNullException.ThrowIfNull(operations);

        MicrosoftGraphBatchSubrequest[] requests = operations
            .Select(operation => new MicrosoftGraphBatchSubrequest(
                operation.Id,
                "GET",
                operation.BatchUrl))
            .ToArray();

        return JsonContent.Create(new MicrosoftGraphBatchRequest(requests));
    }
}