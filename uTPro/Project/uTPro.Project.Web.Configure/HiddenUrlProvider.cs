using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;

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

        /// <summary>
        /// The only container whose transparency is per-node (toggled by editors). All other
        /// containers in <see cref="Aliases"/> are always transparent.
        /// </summary>
        public static readonly string TogglableContainerAlias = GlobalFolderPages.ModelTypeAlias;

        /// <summary>Boolean property alias on the togglable container that, when ON, makes the
        /// container's segment appear in child URLs (e.g. <c>/huong-dan/pages/...</c>).</summary>
        public const string ShowInUrlPropertyAlias = "showInUrl";

        private bool IsShown(string alias, bool showInUrl)
            => alias.Equals(TogglableContainerAlias, StringComparison.OrdinalIgnoreCase) && showInUrl;

        /// <summary>
        /// True when the node should be treated as transparent (dropped from public URLs).
        /// A container is transparent UNLESS it is the togglable container with
        /// <see cref="ShowInUrlPropertyAlias"/> turned ON (in which case its segment is routed
        /// normally and appears in the URL).
        /// </summary>
        public bool IsTransparent(IPublishedContent? node)
        {
            if (node is null || !Aliases.Contains(node.ContentType.Alias))
            {
                return false;
            }

            return !IsShown(node.ContentType.Alias, node.Value<bool>(ShowInUrlPropertyAlias));
        }

        /// <inheritdoc cref="IsTransparent(IPublishedContent?)"/>
        public bool IsTransparent(Umbraco.Cms.Core.Models.IContent? node)
        {
            if (node is null || !Aliases.Contains(node.ContentType.Alias))
            {
                return false;
            }

            return !IsShown(node.ContentType.Alias, node.GetValue<bool>(ShowInUrlPropertyAlias));
        }
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
            => _hidden.IsTransparent(content)
                ? NoUrlMessage(culture)
                : null;

        public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current) => [];

        // No preview URL for transparent container nodes; defer to the default provider otherwise.
        public Task<UrlInfo?> GetPreviewUrlAsync(Umbraco.Cms.Core.Models.IContent content, string? culture, string? segment)
            => Task.FromResult<UrlInfo?>(
                _hidden.IsTransparent(content)
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
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IDocumentUrlService _documentUrlService;

        public HiddenContainerUrlInfoProvider(
            IPublishedUrlInfoProvider inner,
            HiddenContainerAliases hidden,
            IUmbracoContextAccessor umbracoContextAccessor,
            IDocumentUrlService documentUrlService)
        {
            _inner = inner;
            _hidden = hidden;
            _umbracoContextAccessor = umbracoContextAccessor;
            _documentUrlService = documentUrlService;
        }

        public async Task<ISet<UrlInfo>> GetAllAsync(Umbraco.Cms.Core.Models.IContent content)
        {
            // A transparent container has no URL — show a single message instead of any link.
            // (A "shown" togglable container falls through and gets its real URL below.)
            if (_hidden.IsTransparent(content))
            {
                return new HashSet<UrlInfo>
                {
                    UrlInfo.AsMessage("This node is a container and has no URL.", "uTProHiddenUrlProvider", null),
                };
            }

            var urls = await _inner.GetAllAsync(content);

            // For a page that sits under a hidden container, the default provider still leaks the
            // "raw" URL that includes the container segment (e.g. /pages/) via GetOtherUrls — the
            // aggregate GetOtherUrls is a SelectMany, so an IUrlProvider cannot suppress it. Drop
            // any URL that still contains a hidden-container segment; the transparent (clean) URL
            // for each culture is already present in the set.
            var containerSegments = GetAncestorContainerSegments(content, urls);
            if (containerSegments.Count == 0)
            {
                return urls;
            }

            var result = new HashSet<UrlInfo>();
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Pass 1: keep messages and URLs that are already clean (no container segment).
            foreach (var info in urls)
            {
                if (info.Url is null)
                {
                    result.Add(info);
                    continue;
                }

                if (!PathContainsSegment(info.Url, containerSegments))
                {
                    result.Add(info);
                    seenPaths.Add(PathKey(info.Culture, PathOf(info.Url)));
                }
            }

            // Pass 2: for leaked URLs that still contain a container segment (e.g. /pages/),
            // strip the container segment instead of dropping the URL — so the clean variant
            // (e.g. /vi/huong-dan/page-test/) is preserved — and only add it if that clean path
            // isn't already present.
            foreach (var info in urls)
            {
                if (info.Url is null || !PathContainsSegment(info.Url, containerSegments))
                {
                    continue;
                }

                var cleanedPath = StripSegmentsFromPath(PathOf(info.Url), containerSegments);
                if (!seenPaths.Add(PathKey(info.Culture, cleanedPath)))
                {
                    continue;
                }

                var cleanedUrl = info.Url.IsAbsoluteUri
                    ? info.Url.GetLeftPart(UriPartial.Authority) + cleanedPath
                    : cleanedPath;

                var provider = string.IsNullOrEmpty(info.Provider) ? "uTProHiddenUrlProvider" : info.Provider;
                result.Add(UrlInfo.AsUrl(cleanedUrl, provider, info.Culture));
            }

            return result.Count > 0 ? result : urls;
        }

        private static string PathKey(string? culture, string path) => (culture ?? string.Empty) + "|" + path;

        private static string PathOf(Uri url) => url.IsAbsoluteUri ? url.AbsolutePath : url.OriginalString;

        // Removes the given (container) segments from a URL path, preserving a trailing slash.
        private static string StripSegmentsFromPath(string path, HashSet<string> segments)
        {
            var kept = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !segments.Contains(p));

            var result = "/" + string.Join("/", kept);

            if (path.EndsWith('/') && result.Length > 1)
            {
                result += "/";
            }

            return result;
        }

        // URL segments of this content's hidden-container ancestors, resolved for every culture
        // present in the URL set (so both en-US and vi-VN container segments are covered).
        private HashSet<string> GetAncestorContainerSegments(
            Umbraco.Cms.Core.Models.IContent content, ISet<UrlInfo> urls)
        {
            var segments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)
                || umbracoContext.Content is null)
            {
                return segments;
            }

            var node = umbracoContext.Content.GetById(content.Key);
            if (node is null)
            {
                return segments;
            }

            // Only strip segments of TRANSPARENT container ancestors. A "shown" container keeps
            // its segment in child URLs, so it must not be collected here.
            var containers = node.Ancestors().Where(a => _hidden.IsTransparent(a)).ToList();
            if (containers.Count == 0)
            {
                return segments;
            }

            var cultures = urls.Select(u => u.Culture).Distinct().ToList();
            if (cultures.Count == 0)
            {
                cultures.Add(null);
            }

            foreach (var container in containers)
            {
                foreach (var culture in cultures)
                {
                    var segment = _documentUrlService.GetUrlSegment(container.Key, culture ?? string.Empty, false);
                    if (!string.IsNullOrEmpty(segment))
                    {
                        segments.Add(segment);
                    }
                }
            }

            return segments;
        }

        // True when any path segment of the URL equals one of the (container) segments.
        private static bool PathContainsSegment(Uri url, HashSet<string> segments)
        {
            var path = url.IsAbsoluteUri ? url.AbsolutePath : url.OriginalString;

            foreach (var part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (segments.Contains(part))
                {
                    return true;
                }
            }

            return false;
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
                    sp.GetRequiredService<HiddenContainerAliases>(),
                    sp.GetRequiredService<IUmbracoContextAccessor>(),
                    sp.GetRequiredService<IDocumentUrlService>())));

            // Keep hidden-link nodes out of the front-end Examine ExternalIndex so site search
            // never returns them (see HiddenContainerIndexOptions).
            builder.Services.ConfigureOptions<HiddenContainerIndexOptions>();
        }
    }
}
