using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;

namespace uTPro.Project.Web.Configure
{
    /// <summary>
    /// Options for <see cref="HiddenUrlProvider"/>. Bound from the <c>uTPro:HiddenUrls</c> section.
    /// List the document type aliases of container/organisational nodes that are NOT pages and so
    /// should not expose a public URL (the "Links" panel shows a message instead).
    /// </summary>
    public sealed class HiddenUrlOptions
    {
        public const string SectionName = "uTPro:HiddenUrls";

        /// <summary>Document type aliases whose nodes should not generate a URL.</summary>
        public string[] DocumentTypeAliases { get; set; } = [];
    }

    /// <summary>
    /// Suppresses URL generation for container nodes (folders that only organise content and are
    /// not real pages). For a matching document type the provider returns a <see cref="UrlInfo"/>
    /// message instead of a URL, so the backoffice "Links" panel shows an explanation and the node
    /// is not advertised with a navigable link. Non-matching nodes return <c>null</c> so the
    /// default provider handles them normally.
    /// </summary>
    public sealed class HiddenUrlProvider : IUrlProvider
    {
        private readonly HashSet<string> _hiddenAliases;

        /// <summary>
        /// Built-in container document types that are always hidden. These cannot be disabled;
        /// any aliases configured via <see cref="HiddenUrlOptions"/> are added on top of these.
        /// </summary>
        private static readonly string[] DefaultDocumentTypeAliases =
        [
            "globalRoot",
            "globalFolderSites",
            "globalFolderNavigationLinkForSite",
            "globalFolderSettings",
        ];

        public HiddenUrlProvider(IOptions<HiddenUrlOptions> options)
        {
            // Always start from the built-in defaults, then merge in any user-configured aliases
            // so the global containers stay hidden regardless of configuration.
            _hiddenAliases = new HashSet<string>(
                DefaultDocumentTypeAliases,
                StringComparer.OrdinalIgnoreCase);

            foreach (var alias in options.Value.DocumentTypeAliases ?? [])
            {
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    _hiddenAliases.Add(alias);
                }
            }
        }

        public string Alias => "uTProHiddenUrlProvider";

        // A message-type UrlInfo makes the backoffice "Links" panel show text instead of a link.
        // NOTE: AsMessage's second parameter is the provider name (required, non-empty); the culture
        // is the third parameter. Passing the alias avoids the "Parameter 'provider'" ArgumentException.
        private UrlInfo NoUrlMessage(string? culture)
            => UrlInfo.AsMessage("This node is a container and has no URL.", Alias, culture);

        public UrlInfo? GetUrl(IPublishedContent content, UrlMode mode, string? culture, Uri current)
            => _hiddenAliases.Contains(content.ContentType.Alias)
                ? NoUrlMessage(culture)
                : null;

        public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current) => [];

        // No preview URL for container nodes; defer to the default provider for everything else.
        public Task<UrlInfo?> GetPreviewUrlAsync(Umbraco.Cms.Core.Models.IContent content, string? culture, string? segment)
            => Task.FromResult<UrlInfo?>(
                _hiddenAliases.Contains(content.ContentType.Alias)
                    ? NoUrlMessage(culture)
                    : null);
    }

    /// <summary>Registers <see cref="HiddenUrlProvider"/> so it runs before the default provider.</summary>
    public sealed class HiddenUrlComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.Configure<HiddenUrlOptions>(builder.Config.GetSection(HiddenUrlOptions.SectionName));

            // Insert at the front so a "hidden" match takes precedence over the default URL provider.
            builder.UrlProviders().Insert<HiddenUrlProvider>();
        }
    }
}
