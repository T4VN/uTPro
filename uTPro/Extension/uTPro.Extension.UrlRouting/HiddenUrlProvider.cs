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
