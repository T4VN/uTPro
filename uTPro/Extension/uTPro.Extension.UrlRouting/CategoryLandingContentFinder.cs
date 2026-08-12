using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Content finder for category "landing" URLs — e.g. <c>/huong-dan/cau-hinh/</c> where
/// <c>cau-hinh</c> is a category slug with no page segment following it.
/// </summary>
public sealed class CategoryLandingContentFinder(
    CategoryUrlService categoryUrlService,
    ContentTreeWalker treeWalker,
    HiddenContainerAliases hidden,
    IUmbracoContextAccessor umbracoContextAccessor,
    IHttpContextAccessor httpContextAccessor) : IContentFinder
{
    /// <summary>Key used in <see cref="HttpContext.Items"/> to signal a category landing request.</summary>
    public const string CategoryLandingItemKey = "uTPro:CategoryLandingKey";

    /// <summary>
    /// Resolves a category landing URL to its parent page, returning the category key
    /// via <see cref="HttpContext.Items"/> for downstream filtering.
    /// </summary>
    /// <returns><c>true</c> if the request matches a visible category landing URL; <c>false</c> otherwise.</returns>
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
        if (segments.Length < 1)
        {
            return Task.FromResult(false);
        }

        var culture = request.Culture;
        var categorySlugs = categoryUrlService.GetAllVisibleCategorySlugs(culture, root.Key);

        if (categorySlugs.Count == 0)
        {
            return Task.FromResult(false);
        }

        var lastSegment = segments[^1];
        if (!categorySlugs.TryGetValue(lastSegment, out var categoryKey))
        {
            return Task.FromResult(false);
        }

        IPublishedContent parentPage;
        if (segments.Length == 1)
        {
            parentPage = root;
        }
        else
        {
            var parentSegments = segments[..^1];
            var resolved = treeWalker.WalkTree(root, parentSegments, culture);
            if (resolved is null)
            {
                return Task.FromResult(false);
            }

            parentPage = resolved;
        }

        if (hidden.IsTransparent(parentPage))
        {
            return Task.FromResult(false);
        }

        // Verify at least one child of parentPage actually belongs to this category.
        // Without this check, any valid category slug appended to any page returns 200
        // with an empty list, creating phantom pages for search engines.
        var hasMatchingChild = parentPage.Children()?.Any(child =>
            CategoryUrlService.PageHasCategory(child, categoryKey)) is true;

        if (!hasMatchingChild)
        {
            return Task.FromResult(false);
        }

        var httpContext = httpContextAccessor.HttpContext;
        httpContext?.Items[CategoryLandingItemKey] = categoryKey;

        request.SetPublishedContent(parentPage);
        return Task.FromResult(true);
    }
}
