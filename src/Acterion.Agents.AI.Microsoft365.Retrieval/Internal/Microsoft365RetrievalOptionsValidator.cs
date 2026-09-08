using Microsoft.Extensions.Options;

namespace Acterion.Agents.AI.Microsoft365.Retrieval;

internal sealed class Microsoft365RetrievalOptionsValidator : IValidateOptions<Microsoft365RetrievalOptions>
{
    private static readonly Microsoft365RetrievalOptionsValidator Instance = new();

    public ValidateOptionsResult Validate(string? name, Microsoft365RetrievalOptions options)
    {
        List<string> failures = [];

        if (options.MaximumNumberOfResults is < 1 or > 25)
        {
            failures.Add("MaximumNumberOfResults must be between 1 and 25.");
        }

        if (options.ResourceMetadata is null)
        {
            failures.Add("ResourceMetadata must be configured.");
        }
        else if (options.ResourceMetadata.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add("ResourceMetadata cannot contain null, empty, or whitespace-only values.");
        }

        if (options.FilterExpression is not null && string.IsNullOrWhiteSpace(options.FilterExpression))
        {
            failures.Add("FilterExpression must be null or contain a non-whitespace KQL expression.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    internal static Microsoft365RetrievalOptions ValidateAndSnapshot(
        Microsoft365RetrievalOptions options)
    {
        ValidateOptionsResult result = Instance.Validate(Options.DefaultName, options);
        if (result.Failed)
        {
            throw new OptionsValidationException(
                Options.DefaultName,
                typeof(Microsoft365RetrievalOptions),
                result.Failures);
        }

        return new Microsoft365RetrievalOptions
        {
            MaximumNumberOfResults = options.MaximumNumberOfResults,
            FilterExpression = options.FilterExpression,
            ResourceMetadata = options.ResourceMetadata.ToArray(),
        };
    }
}