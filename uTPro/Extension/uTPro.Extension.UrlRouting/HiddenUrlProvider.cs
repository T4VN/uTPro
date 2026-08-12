using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Suppresses URL generation for container nodes (folders that only organise content).
/// </summary>
public sealed class HiddenUrlProvider : IUrlProvider
{
    private readonly HiddenContainerAliases _hidden;

    /// <summary>
    /// Initializes a provider that suppresses URLs for content identified as transparent containers.
    /// </summary>
    /// <param name="hidden">The service used to identify transparent containers.</param>
    public HiddenUrlProvider(HiddenContainerAliases hidden)
    {
        _hidden = hidden;
    }

    public string Alias => "uTProHiddenUrlProvider";

    /// <summary>
        /// Creates a URL information message indicating that the node has no URL.
        /// </summary>
        /// <param name="culture">The culture associated with the message.</param>
        /// <returns>A URL information message for the container node.</returns>
        private UrlInfo NoUrlMessage(string? culture)
        => UrlInfo.AsMessage("This node is a container and has no URL.", Alias, culture);

    /// <summary>
            /// Provides a no-URL message for transparent content.
            /// </summary>
            /// <param name="content">The published content whose URL is requested.</param>
            /// <param name="mode">The URL generation mode.</param>
            /// <param name="culture">The culture for the URL.</param>
            /// <param name="current">The current URI.</param>
            /// <returns>A no-URL result for transparent content; otherwise, <c>null</c>.</returns>
            public UrlInfo? GetUrl(IPublishedContent content, UrlMode mode, string? culture, Uri current)
        => _hidden.IsTransparent(content)
            ? NoUrlMessage(culture)
            : null;

    /// <summary>
/// Provides alternate URLs for the specified content item.
/// </summary>
/// <param name="id">The content item identifier.</param>
/// <param name="current">The current URL.</param>
/// <returns>An empty collection.</returns>
public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current) => [];

    /// <summary>
                /// Provides a no-URL message for transparent content during preview URL generation.
                /// </summary>
                /// <param name="content">The content whose preview URL is requested.</param>
                /// <param name="culture">The culture associated with the preview URL.</param>
                /// <param name="segment">The content segment associated with the preview URL.</param>
                /// <returns>A no-URL message for transparent content; otherwise, null.</returns>
                public Task<UrlInfo?> GetPreviewUrlAsync(Umbraco.Cms.Core.Models.IContent content, string? culture, string? segment)
        => Task.FromResult<UrlInfo?>(
            _hidden.IsTransparent(content)
                ? NoUrlMessage(culture)
                : null);
}

/// <summary>
/// Decorates the built-in <see cref="IPublishedUrlInfoProvider"/> so that hidden container nodes
/// report a single "no URL" message instead of any navigable URL.
/// </summary>
public sealed class HiddenContainerUrlInfoProvider : IPublishedUrlInfoProvider
{
    private readonly IPublishedUrlInfoProvider _inner;
    private readonly HiddenContainerAliases _hidden;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IDocumentUrlService _documentUrlService;

    /// <summary>
    /// Initializes a provider that removes URL path segments belonging to transparent container ancestors.
    /// </summary>
    /// <param name="inner">The provider used to retrieve published URL information.</param>
    /// <param name="hidden">The aliases that identify transparent containers.</param>
    /// <param name="umbracoContextAccessor">Provides access to the current Umbraco context.</param>
    /// <param name="documentUrlService">Provides document URL information for transparent ancestors.</param>
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

    /// <summary>
    /// Retrieves URL information while suppressing transparent container URLs and removing transparent ancestor segments from descendant URLs.
    /// </summary>
    /// <param name="content">The content whose URL information is retrieved.</param>
    /// <returns>
    /// URL information for the content, including a no-URL message for transparent containers and cleaned URLs for descendants of transparent containers.
    /// </returns>
    public async Task<ISet<UrlInfo>> GetAllAsync(Umbraco.Cms.Core.Models.IContent content)
    {
        if (_hidden.IsTransparent(content))
        {
            return new HashSet<UrlInfo>
            {
                UrlInfo.AsMessage("This node is a container and has no URL.", "uTProHiddenUrlProvider", null),
            };
        }

        var urls = await _inner.GetAllAsync(content);

        var containerSegments = GetAncestorContainerSegments(content, urls);
        if (containerSegments.Count == 0)
        {
            return urls;
        }

        var result = new HashSet<UrlInfo>();
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

    /// <summary>
/// Extracts the path from a URI.
/// </summary>
/// <param name="url">The URI whose path is extracted.</param>
/// <returns>The absolute path for an absolute URI, or the original string for a relative URI.</returns>
private static string PathOf(Uri url) => url.IsAbsoluteUri ? url.AbsolutePath : url.OriginalString;

    /// <summary>
    /// Removes the specified segments from a URL path while preserving its leading slash and applicable trailing slash.
    /// </summary>
    /// <param name="path">The URL path to clean.</param>
    /// <param name="segments">The path segments to remove.</param>
    /// <returns>The path with matching segments removed.</returns>
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

    /// <summary>
    /// Gets URL path segments for transparent ancestor containers represented by the supplied URLs.
    /// </summary>
    /// <param name="content">The content whose transparent ancestors are examined.</param>
    /// <param name="urls">The existing URLs used to determine the relevant cultures.</param>
    /// <returns>The case-insensitive set of transparent ancestor URL segments.</returns>
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

    /// <summary>
    /// Determines whether a URL path contains any of the specified segments.
    /// </summary>
    /// <param name="url">The URL whose path is checked.</param>
    /// <param name="segments">The path segments to find.</param>
    /// <returns><c>true</c> if the URL path contains a specified segment; <c>false</c> otherwise.</returns>
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
