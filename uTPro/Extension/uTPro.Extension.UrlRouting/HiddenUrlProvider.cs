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
    /// <summary>
    /// Retrieves URLs for content while removing URL segments belonging to transparent ancestor containers.
    /// </summary>
    /// <param name="content">The content whose URLs are retrieved.</param>
    /// <returns>
    /// The content URLs with transparent ancestor segments removed, or a no-URL message for transparent content.
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
    /// Strips container segments from the path. Uses the ordered ancestor list to determine
    /// which segments to remove, accounting for domain-root offset (the URL may not contain
    /// segments for ancestors above the domain root).
    /// </summary>
    private static string StripSegmentsFromPath(string path, IReadOnlyList<string?> ancestorSegments)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();

        // Find the offset where ancestor segments start appearing in the path.
        // Ancestors above the domain root won't appear in the URL, so we need to
        // find where the first non-null ancestor segment actually matches in the path.
        var offset = FindAncestorOffset(parts, ancestorSegments);

        // Walk backwards to avoid index shift issues when removing.
        for (var i = Math.Min(ancestorSegments.Count, parts.Count + offset) - 1; i >= offset; i--)
        {
            if (ancestorSegments[i] is { } s)
            {
                var pathIndex = i - offset;
                if (pathIndex >= 0 && pathIndex < parts.Count
                    && string.Equals(parts[pathIndex], s, StringComparison.OrdinalIgnoreCase))
                {
                    parts.RemoveAt(pathIndex);
                }
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
    /// Determines the offset in <paramref name="ancestorSegments"/> where the path begins.
    /// Ancestors above the domain root don't appear in the URL, so we scan for the first
    /// non-null ancestor segment that matches a path part at a consistent offset.
    /// </summary>
    private static int FindAncestorOffset(List<string> parts, IReadOnlyList<string?> ancestorSegments)
    {
        // Try each possible offset: how many ancestor entries are "above" the URL
        for (var offset = 0; offset < ancestorSegments.Count; offset++)
        {
            var match = true;
            var foundAny = false;

            for (var i = offset; i < ancestorSegments.Count; i++)
            {
                var pathIndex = i - offset;
                if (pathIndex >= parts.Count) break;

                if (ancestorSegments[i] is null)
                {
                    // Non-container ancestor — its segment should be in the URL
                    continue;
                }

                foundAny = true;
                if (!string.Equals(parts[pathIndex], ancestorSegments[i], StringComparison.OrdinalIgnoreCase))
                {
                    match = false;
                    break;
                }
            }

            if (match && foundAny)
            {
                return offset;
            }
        }

        return 0;
    }

    /// <summary>
    /// Builds an ordered list (root→leaf) of ancestor URL segments. Entries are null for
    /// non-container ancestors, and non-null for transparent containers. This preserves
    /// positional information needed by <see cref="StripSegmentsFromPath"/>.
    /// <summary>
    /// Builds the ordered URL segment list for the specified ancestors.
    /// </summary>
    /// <param name="ancestors">The ancestors and whether each one is a transparent container.</param>
    /// <param name="culture">The culture used to resolve transparent ancestor URL segments.</param>
    /// <returns>A positional list containing URL segments for transparent ancestors and null entries for other ancestors.</returns>
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
    /// <summary>
    /// Gets the content's ancestors in root-to-leaf order with their transparency status.
    /// </summary>
    /// <param name="content">The content whose ancestors are resolved.</param>
    /// <returns>
    /// The ordered ancestors and their transparency status, or <c>null</c> if the content cannot be resolved
    /// or has no transparent ancestors.
    /// </returns>
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

    /// <summary>
    /// Determines whether a URL path contains a transparent ancestor segment at its expected position,
    /// accounting for domain-root offset.
    /// </summary>
    private static bool PathContainsSegment(Uri url, IReadOnlyList<string?> ancestorSegments)
    {
        var path = url.IsAbsoluteUri ? url.AbsolutePath : url.OriginalString;
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();

        var offset = FindAncestorOffset(parts, ancestorSegments);

        for (var i = offset; i < ancestorSegments.Count; i++)
        {
            var pathIndex = i - offset;
            if (pathIndex >= parts.Count) break;

            if (ancestorSegments[i] is { } s
                && string.Equals(parts[pathIndex], s, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
