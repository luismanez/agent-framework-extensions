namespace Acterion.Agents.AI.Microsoft365.WorkContext;

internal sealed record MicrosoftGraphOperation(
    string Id,
    WorkContextFacet Facet,
    Uri DirectUri,
    string BatchUrl);