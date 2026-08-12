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

    public HiddenContainerValueSetValidator(HiddenContainerAliases hidden, IValueSetValidator? inner)
    {
        _hidden = hidden;
        _inner = inner;
    }

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

    public HiddenContainerIndexOptions(HiddenContainerAliases hidden)
    {
        _hidden = hidden;
    }

    public void Configure(string? name, LuceneDirectoryIndexOptions options)
    {
        if (name != Constants.UmbracoIndexes.ExternalIndexName)
        {
            return;
        }

        options.Validator = new HiddenContainerValueSetValidator(_hidden, options.Validator);
    }

    public void Configure(LuceneDirectoryIndexOptions options) => Configure(Options.DefaultName, options);
}
