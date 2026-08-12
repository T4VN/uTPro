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
    /// <summary>
    /// Resolves a published page from a URL containing a visible category segment.
    /// </summary>
    /// <returns><c>true</c> if a matching page is found and assigned to the category; <c>false</c> otherwise.</returns>
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

        // Split path first, then decode each segment individually.
        // This prevents encoded slashes (%2F) from being decoded into '/' and creating extra segments.
        var rawPath = request.Uri.AbsolutePath.Trim('/');
        var domainPath = request.Domain.Uri?.AbsolutePath.Trim('/') ?? string.Empty;
        rawPath = RequestPathHelper.StripDomainPrefix(rawPath, domainPath);

        if (rawPath.Length == 0)
        {
            return Task.FromResult(false);
        }

        var segments = rawPath.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();
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

        // CategoryUrlProvider always places the category segment immediately before
        // the page segment (penultimate position). Only accept that canonical position
        // to avoid multiple URLs resolving to the same content (duplicate content issue).
        var categoryIndex = segments.Length - 2;
        if (!categorySlugs.TryGetValue(segments[categoryIndex], out var categoryKey))
        {
            return Task.FromResult(false);
        }

        var withoutCategory = segments.Where((_, idx) => idx != categoryIndex).ToArray();

        var resolved = treeWalker.WalkTree(root, withoutCategory, culture);
        if (resolved is null)
        {
            return Task.FromResult(false);
        }

        if (!CategoryUrlService.PageHasCategory(resolved, categoryKey))
        {
            return Task.FromResult(false);
        }

        request.SetPublishedContent(resolved);
        return Task.FromResult(true);
    }
}
