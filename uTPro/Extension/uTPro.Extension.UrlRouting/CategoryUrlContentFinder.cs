using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Inbound content finder: resolves URLs that contain a category segment.
/// </summary>
public sealed class CategoryUrlContentFinder(
    CategoryUrlService categoryUrlService,
    ContentTreeWalker treeWalker,
    IUmbracoContextAccessor umbracoContextAccessor) : IContentFinder
{
    /// <summary>
    /// Resolves a published page from a URL containing a visible category segment.
    /// </summary>
    /// <param name="request">The published request to resolve.</param>
    /// <returns><c>true</c> if a matching page is found; <c>false</c> otherwise.</returns>
    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        if (request.PublishedContent is not null)
        {
            return Task.FromResult(false);
        }

        if (request.Domain is null)
        {
            return Task.FromResult(false);
        }

        if (!umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)
            || umbracoContext.Content is null)
        {
            return Task.FromResult(false);
        }

        var root = umbracoContext.Content.GetById(request.Domain.ContentId);
        if (root is null)
        {
            return Task.FromResult(false);
        }

        var decodedPath = Uri.UnescapeDataString(request.Uri.AbsolutePath).Trim('/');
        var domainPath = request.Domain.Uri?.AbsolutePath.Trim('/') ?? string.Empty;
        if (domainPath.Length > 0
            && decodedPath.StartsWith(domainPath, StringComparison.OrdinalIgnoreCase))
        {
            decodedPath = decodedPath[domainPath.Length..].Trim('/');
        }

        if (decodedPath.Length == 0)
        {
            return Task.FromResult(false);
        }

        var segments = decodedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return Task.FromResult(false);
        }

        var culture = request.Culture;
        var categorySlugs = categoryUrlService.GetAllVisibleCategorySlugs(culture, root.Key);

        if (categorySlugs.Count == 0)
        {
            return Task.FromResult(false);
        }

        for (int i = 0; i < segments.Length; i++)
        {
            if (!categorySlugs.TryGetValue(segments[i], out var categoryKey))
            {
                continue;
            }

            var withoutCategory = segments.Where((_, idx) => idx != i).ToArray();

            var resolved = treeWalker.WalkTree(root, withoutCategory, culture);
            if (resolved is null)
            {
                continue;
            }

            if (!CategoryUrlService.PageHasCategory(resolved, categoryKey))
            {
                continue;
            }

            request.SetPublishedContent(resolved);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
