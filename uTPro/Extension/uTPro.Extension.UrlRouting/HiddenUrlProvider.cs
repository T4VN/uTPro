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

    public HiddenUrlProvider(HiddenContainerAliases hidden)
    {
        _hidden = hidden;
    }

    public string Alias => "uTProHiddenUrlProvider";

    private UrlInfo NoUrlMessage(string? culture)
        => UrlInfo.AsMessage("This node is a container and has no URL.", Alias, culture);

    public UrlInfo? GetUrl(IPublishedContent content, UrlMode mode, string? culture, Uri current)
        => _hidden.IsTransparent(content)
            ? NoUrlMessage(culture)
            : null;

    public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current) => [];

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
    private IReadOnlyList<string?> GetAncestorContainerSegments(
        Umbraco.Cms.Core.Models.IContent content, ISet<UrlInfo> urls)
    {
        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)
            || umbracoContext.Content is null)
        {
            return [];
        }

        var node = umbracoContext.Content.GetById(content.Key);
        if (node is null)
        {
            return [];
        }

        // Ancestors() returns parent→root order; reverse to get root→leaf.
        var ancestors = node.Ancestors().Reverse().ToList();
        if (!ancestors.Any(a => _hidden.IsTransparent(a)))
        {
            return [];
        }

        // Determine the primary culture for segment resolution
        var culture = urls.Select(u => u.Culture).FirstOrDefault(c => c is not null) ?? string.Empty;

        var result = new List<string?>(ancestors.Count);
        foreach (var ancestor in ancestors)
        {
            if (_hidden.IsTransparent(ancestor))
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
