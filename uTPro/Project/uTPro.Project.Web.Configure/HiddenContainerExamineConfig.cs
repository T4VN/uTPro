using Examine;
using Examine.Lucene;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;

namespace uTPro.Project.Web.Configure
{
    /// <summary>
    /// Keeps "hidden link" nodes out of front-end search results. Any node whose document type is a
    /// hidden container (see <see cref="HiddenContainerAliases"/> — the same single source of truth
    /// used by <see cref="HiddenUrlProvider"/> to suppress the node's URL) is excluded from the
    /// Examine <c>ExternalIndex</c>, which is the index the public site searches against.
    ///
    /// <para>Because it reuses <see cref="HiddenContainerAliases"/>, the built-in container types and
    /// any aliases configured via <c>uTPro:HiddenUrls</c> are automatically excluded — no separate
    /// list to maintain.</para>
    ///
    /// <para>Only the <c>ExternalIndex</c> is touched, so the backoffice (<c>InternalIndex</c>) can
    /// still find and manage these nodes in the content tree. Existing indexed items are removed the
    /// next time the ExternalIndex is rebuilt.</para>
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
            // For content, ValueSet.ItemType is the document type alias. Drop the whole item when it
            // is a hidden container so it never lands in the index.
            if (_hidden.Contains(valueSet.ItemType))
            {
                return new ValueSetValidationResult(ValueSetValidationStatus.Failed, valueSet);
            }

            // Otherwise defer to the index's original validator (published-only / protected-content
            // rules etc.) so we don't lose the default behaviour.
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
}
