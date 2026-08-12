using Examine;
using Examine.Lucene;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Keeps "hidden link" nodes out of the Examine <c>ExternalIndex</c>.
/// </summary>
public sealed class HiddenContainerValueSetValidator : IValueSetValidator
{
    private readonly HiddenContainerAliases _hidden;
    private readonly IValueSetValidator? _inner;

    /// <summary>
    /// Initializes a validator that excludes value sets associated with hidden containers and delegates other validation to an optional inner validator.
    /// </summary>
    /// <param name="hidden">The aliases identifying hidden containers.</param>
    /// <param name="inner">The validator to use for value sets that are not hidden.</param>
    public HiddenContainerValueSetValidator(HiddenContainerAliases hidden, IValueSetValidator? inner)
    {
        _hidden = hidden;
        _inner = inner;
    }

    /// <summary>
    /// Validates a value set against the hidden container aliases.
    /// </summary>
    /// <param name="valueSet">The value set to validate.</param>
    /// <returns>A failed result when the value set's item type is hidden; otherwise, the inner validator's result or a valid result.</returns>
    public ValueSetValidationResult Validate(ValueSet valueSet)
    {
        if (_hidden.Contains(valueSet.ItemType))
        {
            return new ValueSetValidationResult(ValueSetValidationStatus.Failed, valueSet);
        }

        return _inner?.Validate(valueSet)
            ?? new ValueSetValidationResult(ValueSetValidationStatus.Valid, valueSet);
    }
}

/// <summary>
/// Wraps the <c>ExternalIndex</c>'s validator with <see cref="HiddenContainerValueSetValidator"/>.
/// </summary>
public sealed class HiddenContainerIndexOptions : IConfigureNamedOptions<LuceneDirectoryIndexOptions>
{
    private readonly HiddenContainerAliases _hidden;

    /// <summary>
    /// Initializes a new instance configured with the hidden container aliases.
    /// </summary>
    /// <param name="hidden">The aliases used to identify hidden containers.</param>
    public HiddenContainerIndexOptions(HiddenContainerAliases hidden)
    {
        _hidden = hidden;
    }

    /// <summary>
    /// Configures the external index to exclude value sets for hidden containers.
    /// </summary>
    /// <param name="name">The name of the index being configured.</param>
    /// <param name="options">The options for the named index.</param>
    public void Configure(string? name, LuceneDirectoryIndexOptions options)
    {
        if (name != Constants.UmbracoIndexes.ExternalIndexName)
        {
            return;
        }

        options.Validator = new HiddenContainerValueSetValidator(_hidden, options.Validator);
    }

    /// <summary>
/// Configures the default named Lucene directory index options.
/// </summary>
/// <param name="options">The index options to configure.</param>
public void Configure(LuceneDirectoryIndexOptions options) => Configure(Options.DefaultName, options);
}
