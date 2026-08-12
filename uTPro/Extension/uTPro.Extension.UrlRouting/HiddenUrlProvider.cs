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

        var ancestorContainers = GetAncestorContainers(content);
        if (ancestorContainers is null)
        {
            return urls;
        }

        // Cache ancestor segments per culture to avoid redundant GetUrlSegment calls
        var segmentsByCulture = new Dictionary<string, IReadOnlyList<string?>>(StringComparer.OrdinalIgnoreCase);

        IReadOnlyList<string?> GetSegmentsForCulture(string? culture)
        {
            var key = culture ?? string.Empty;
            if (!segmentsByCulture.TryGetValue(key, out var segments))
            {
                segments = BuildAncestorSegments(ancestorContainers, key);
                segmentsByCulture[key] = segments;
            }
            return segments;
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

            var ancestorSegments = GetSegmentsForCulture(info.Culture);
            if (!PathContainsSegment(info.Url, ancestorSegments))
            {
                result.Add(info);
                seenPaths.Add(PathKey(info.Culture, PathOf(info.Url)));
            }
        }

        foreach (var info in urls)
        {
            if (info.Url is null)
            {
                continue;
            }

            var ancestorSegments = GetSegmentsForCulture(info.Culture);
            if (!PathContainsSegment(info.Url, ancestorSegments))
            {
                continue;
            }

            var cleanedPath = StripSegmentsFromPath(PathOf(info.Url), ancestorSegments);
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
    /// Strips container segments from the path by position. Container ancestors appear
    /// from the root down, so their segments occupy prefix positions in the path.
    /// Only removes a segment when it matches the expected container at that position,
    /// preventing false removal of legitimate page segments that happen to share the name.
    /// </summary>
    private static string StripSegmentsFromPath(string path, IReadOnlyList<string?> ancestorSegments)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();

        // ancestorSegments is ordered root→down. Null entries represent non-container ancestors.
        // Walk from the beginning of the path: for each ancestor segment, if the path part matches
        // at that position, remove it (shift remaining parts left).
        for (var i = Math.Min(ancestorSegments.Count, parts.Count) - 1; i >= 0; i--)
        {
            if (ancestorSegments[i] is { } s
                && string.Equals(parts[i], s, StringComparison.OrdinalIgnoreCase))
            {
                parts.RemoveAt(i);
            }
        }

        var result = "/" + string.Join("/", parts);

        if (path.EndsWith('/') && result.Length > 1)
        {
            result += "/";
        }

        return result;
    }

    /// <summary>
    /// Builds an ordered list (root→leaf) of ancestor URL segments. Entries are null for
    /// non-container ancestors, and non-null for transparent containers. This preserves
    /// positional information needed by <see cref="StripSegmentsFromPath"/>.
    /// </summary>
    private IReadOnlyList<string?> BuildAncestorSegments(
        IReadOnlyList<(IPublishedContent Ancestor, bool IsTransparent)> ancestors, string culture)
    {
        var result = new List<string?>(ancestors.Count);
        foreach (var (ancestor, isTransparent) in ancestors)
        {
            if (isTransparent)
            {
                var segment = _documentUrlService.GetUrlSegment(ancestor.Key, culture, false);
                result.Add(segment);
            }
            else
            {
                result.Add(null);
            }
        }
        return result;
    }

    /// <summary>
    /// Gets the ordered ancestor list with transparency flags. Returns null if no transparent
    /// containers exist in the ancestry (indicating no work to do).
    /// </summary>
    private IReadOnlyList<(IPublishedContent Ancestor, bool IsTransparent)>? GetAncestorContainers(
        Umbraco.Cms.Core.Models.IContent content)
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)
            || umbracoContext.Content is null)
        {
            return null;
        }

        var node = umbracoContext.Content.GetById(content.Key);
        if (node is null)
        {
            return null;
        }

        // Ancestors() returns parent→root order; reverse to get root→leaf.
        var ancestors = node.Ancestors().Reverse().ToList();
        if (!ancestors.Any(a => _hidden.IsTransparent(a)))
        {
            return null;
        }

        return ancestors.Select(a => (a, _hidden.IsTransparent(a))).ToList();
    }

    private static bool PathContainsSegment(Uri url, IReadOnlyList<string?> ancestorSegments)
    {
        var path = url.IsAbsoluteUri ? url.AbsolutePath : url.OriginalString;
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        for (var i = 0; i < Math.Min(ancestorSegments.Count, parts.Length); i++)
        {
            if (ancestorSegments[i] is { } s
                && string.Equals(parts[i], s, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
