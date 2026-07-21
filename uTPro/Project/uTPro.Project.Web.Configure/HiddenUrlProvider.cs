using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Web.Common.PublishedModels;

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
    /// Resolved set of "container" document type aliases (built-in defaults + configured extras).
    /// Shared by <see cref="HiddenUrlProvider"/> (suppresses the container's own URL) and the
    /// transparent-routing classes (<see cref="TransparentContainerUrlProvider"/> /
    /// <see cref="TransparentContainerContentFinder"/>), so both features stay in sync from a
    /// single source of truth.
    /// </summary>
    public sealed class HiddenContainerAliases
    {
        /// <summary>
        /// Built-in container document types that are always hidden. These cannot be disabled;
        /// any aliases configured via <see cref="HiddenUrlOptions"/> are added on top of these.
        /// </summary>
        private static readonly string[] DefaultDocumentTypeAliases =
        [
            GlobalFolderRoot.ModelTypeAlias,
            GlobalFolderSites.ModelTypeAlias,
            GlobalFolderNavigationLinkForSite.ModelTypeAlias,
            GlobalFolderSettings.ModelTypeAlias,
            GlobalFolderPages.ModelTypeAlias,
        ];

        public HiddenContainerAliases(IOptions<HiddenUrlOptions> options)
        {
            var set = new HashSet<string>(DefaultDocumentTypeAliases, StringComparer.OrdinalIgnoreCase);

            foreach (var alias in options.Value.DocumentTypeAliases ?? [])
            {
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    set.Add(alias);
                }
            }

            Aliases = set;
        }

        /// <summary>The resolved, case-insensitive set of hidden container aliases.</summary>
        public IReadOnlySet<string> Aliases { get; }

        /// <summary>True when the given document type alias is a hidden container.</summary>
        public bool Contains(string? alias) => alias is not null && Aliases.Contains(alias);
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
        private readonly HiddenContainerAliases _hidden;

        public HiddenUrlProvider(HiddenContainerAliases hidden)
        {
            _hidden = hidden;
        }

        public string Alias => "uTProHiddenUrlProvider";

        // A message-type UrlInfo makes the backoffice "Links" panel show text instead of a link.
        // NOTE: AsMessage's second parameter is the provider name (required, non-empty); the culture
        // is the third parameter. Passing the alias avoids the "Parameter 'provider'" ArgumentException.
        private UrlInfo NoUrlMessage(string? culture)
            => UrlInfo.AsMessage("This node is a container and has no URL.", Alias, culture);

        public UrlInfo? GetUrl(IPublishedContent content, UrlMode mode, string? culture, Uri current)
            => _hidden.Contains(content.ContentType.Alias)
                ? NoUrlMessage(culture)
                : null;

        public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current) => [];

        // No preview URL for container nodes; defer to the default provider for everything else.
        public Task<UrlInfo?> GetPreviewUrlAsync(Umbraco.Cms.Core.Models.IContent content, string? culture, string? segment)
            => Task.FromResult<UrlInfo?>(
                _hidden.Contains(content.ContentType.Alias)
                    ? NoUrlMessage(culture)
                    : null);
    }

    /// <summary>
    /// Decorates the built-in <see cref="IPublishedUrlInfoProvider"/> (the service the backoffice
    /// "Links"/"Info" panel calls via <c>DocumentUrlFactory</c>) so that hidden container nodes
    /// report a single "no URL" message instead of any navigable URL.
    ///
    /// <para>Why this is needed in addition to <see cref="HiddenUrlProvider"/>: the backoffice does
    /// NOT just call <c>IPublishedUrlProvider.GetUrl</c> (first-non-null, which our provider can
    /// intercept). <c>PublishedUrlInfoProvider.GetAllAsync</c> ALSO appends
    /// <c>IPublishedUrlProvider.GetOtherUrls</c>, and the aggregate <c>GetOtherUrls</c> is a
    /// <c>SelectMany</c> across every provider. The default provider still emits the container's
    /// routable URLs there (e.g. <c>/en/tutorial/pages/</c>), and an <see cref="IUrlProvider"/>
    /// cannot suppress another provider's <c>GetOtherUrls</c> output. Intercepting at the
    /// <see cref="IPublishedUrlInfoProvider"/> level short-circuits that whole flow.</para>
    /// </summary>
    public sealed class HiddenContainerUrlInfoProvider : IPublishedUrlInfoProvider
    {
        private readonly IPublishedUrlInfoProvider _inner;
        private readonly HiddenContainerAliases _hidden;

        public HiddenContainerUrlInfoProvider(IPublishedUrlInfoProvider inner, HiddenContainerAliases hidden)
        {
            _inner = inner;
            _hidden = hidden;
        }

        public Task<ISet<UrlInfo>> GetAllAsync(Umbraco.Cms.Core.Models.IContent content)
        {
            if (_hidden.Contains(content.ContentType.Alias))
            {
                // A message-type UrlInfo makes the panel show text instead of a navigable link.
                // (Return an empty set instead if you prefer the panel to show no rows at all.)
                ISet<UrlInfo> message = new HashSet<UrlInfo>
                {
                    UrlInfo.AsMessage("This node is a container and has no URL.", "uTProHiddenUrlProvider", null),
                };

                return Task.FromResult(message);
            }

            return _inner.GetAllAsync(content);
        }
    }

    /// <summary>Registers <see cref="HiddenUrlProvider"/> so it runs before the default provider.</summary>
    public sealed class HiddenUrlComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.Configure<HiddenUrlOptions>(builder.Config.GetSection(HiddenUrlOptions.SectionName));
            builder.Services.TryAddSingleton<HiddenContainerAliases>();

            // Insert at the front so a "hidden" match takes precedence over the default URL provider.
            // This suppresses the container's own URL for front-end GetUrl() calls (returns "#").
            builder.UrlProviders().Insert<HiddenUrlProvider>();

            // Backoffice "Links" panel: decorate IPublishedUrlInfoProvider so container nodes never
            // show a navigable URL there. Necessary because that panel also aggregates GetOtherUrls
            // from the default provider, which an IUrlProvider alone cannot suppress. Register the
            // built-in implementation as a concrete type so the decorator can delegate to it.
            builder.Services.AddTransient<PublishedUrlInfoProvider>();
            builder.Services.Replace(ServiceDescriptor.Transient<IPublishedUrlInfoProvider>(sp =>
                new HiddenContainerUrlInfoProvider(
                    sp.GetRequiredService<PublishedUrlInfoProvider>(),
                    sp.GetRequiredService<HiddenContainerAliases>())));

            // Keep hidden-link nodes out of the front-end Examine ExternalIndex so site search
            // never returns them (see HiddenContainerIndexOptions).
            builder.Services.ConfigureOptions<HiddenContainerIndexOptions>();
        }
    }
}
