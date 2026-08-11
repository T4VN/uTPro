using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Service that resolves category URL segments for pages. Shared by the outbound URL provider
/// and the inbound content finder.
/// </summary>
public sealed class CategoryUrlService(
    IUmbracoContextAccessor umbracoContextAccessor,
    IDocumentUrlService documentUrlService,
    IVariationContextAccessor variationContextAccessor)
{
    /// <summary>
    /// Gets all category items assigned to the given page that have <c>showInUrl</c> enabled.
    /// Returns them in picker order.
    /// </summary>
    public static IReadOnlyList<IPublishedContent> GetVisibleCategories(IPublishedContent page)
    {
        var categories = page.Value<IEnumerable<IPublishedContent>>(CategoryUrlConstants.PageCategoriesAlias);
        if (categories is null)
        {
            return [];
        }

        return [.. categories
            .Where(c => c?.ContentType?.Alias is not null
                && c.ContentType.Alias.Equals(GlobalFolderCategoryItem.ModelTypeAlias, StringComparison.OrdinalIgnoreCase))
            .Where(c => c.Value<bool>(CategoryUrlConstants.ShowInUrlAlias))];
    }

    /// <summary>
    /// Gets the URL segment for a category item.
    /// </summary>
    public string? GetCategorySegment(IPublishedContent? categoryItem, string? culture)
    {
        if (categoryItem is null || categoryItem.Key == Guid.Empty)
        {
            return null;
        }

        var customSegment = categoryItem.Value<string>(CategoryUrlConstants.UrlSegmentAlias);
        if (!string.IsNullOrWhiteSpace(customSegment))
        {
            return customSegment.Trim().ToLowerInvariant();
        }

        return documentUrlService.GetUrlSegment(
            categoryItem.Key,
            culture ?? variationContextAccessor.VariationContext?.Culture ?? string.Empty,
            false);
    }

    /// <summary>
    /// Gets all visible category slugs under a given site root.
    /// </summary>
    public Dictionary<string, Guid> GetAllVisibleCategorySlugs(string? culture, IPublishedContent? siteRoot = null)
    {
        if (!umbracoContextAccessor.TryGetUmbracoContext(out var ctx) || ctx.Content is null)
        {
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        if (siteRoot is not null)
        {
            CollectCategoryItems(siteRoot, culture, result);
        }

        return result;
    }

    /// <summary>
    /// Overload that accepts a domain root key.
    /// </summary>
    public Dictionary<string, Guid> GetAllVisibleCategorySlugs(string? culture, Guid domainRootKey)
    {
        if (!umbracoContextAccessor.TryGetUmbracoContext(out var ctx) || ctx.Content is null)
        {
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        }

        var domainRoot = ctx.Content.GetById(domainRootKey);
        if (domainRoot is null)
        {
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        var current = domainRoot;
        while (current is not null)
        {
            CollectCategoryItems(current, culture, result);
            if (result.Count > 0)
            {
                break;
            }

            current = current.Parent();
        }

        return result;
    }

    /// <summary>
    /// Gets the category landing URL and category name for a given page.
    /// </summary>
    public (string Url, string Name) GetUrlNameCategory(IPublishedContent currentNode, IPublishedContent? currentCat = null)
    {
        if (currentNode?.ContentType?.Alias is null)
        {
            return (string.Empty, string.Empty);
        }

        var crumbs = currentNode.AncestorsOrSelf()
            .Where(n =>
            {
                var u = n.Url(mode: UrlMode.Relative);
                return !string.IsNullOrEmpty(u) && u != "#";
            })
            .Reverse()
            .ToList();

        if (crumbs.Count == 0)
        {
            return (string.Empty, string.Empty);
        }

        IPublishedContent? targetCat = currentCat;
        if (targetCat is null)
        {
            var visibleCategories = GetVisibleCategories(currentNode);
            targetCat = visibleCategories.Count > 0 ? visibleCategories[0] : null;
        }

        if (targetCat is null)
        {
            return (string.Empty, string.Empty);
        }

        var categoryCrumbName = targetCat.Name ?? string.Empty;
        var catSegment = GetCategorySegment(targetCat, null);
        if (string.IsNullOrEmpty(catSegment))
        {
            return (string.Empty, categoryCrumbName);
        }

        var hasChildren = currentNode.Children()?.Any() == true;
        var basePage = hasChildren ? currentNode : (crumbs.Count >= 2 ? crumbs[^2] : null);
        var baseUrl = basePage?.Url()?.TrimEnd('/') ?? "";

        var categoryCrumbUrl = $"{baseUrl}/{catSegment}/";
        return (categoryCrumbUrl, categoryCrumbName);
    }

    /// <summary>
    /// Checks if a page references a category with the given key.
    /// </summary>
    public static bool PageHasCategory(IPublishedContent page, Guid categoryKey)
    {
        var categories = page.Value<IEnumerable<IPublishedContent>>(CategoryUrlConstants.PageCategoriesAlias);
        return categories?.Any(c => c is not null && c.Key == categoryKey) == true;
    }

    private void CollectCategoryItems(IPublishedContent node, string? culture, Dictionary<string, Guid> result)
    {
        if (node?.ContentType?.Alias is null)
        {
            return;
        }

        if (node.ContentType.Alias.Equals(GlobalFolderCategoryItem.ModelTypeAlias, StringComparison.OrdinalIgnoreCase))
        {
            if (node.Key != Guid.Empty && node.Value<bool>(CategoryUrlConstants.ShowInUrlAlias))
            {
                var segment = GetCategorySegment(node, culture);
                if (!string.IsNullOrEmpty(segment) && !result.ContainsKey(segment))
                {
                    result[segment] = node.Key;
                }
            }

            return;
        }

        var alias = node.ContentType.Alias;
        if (alias.StartsWith("globalFolder", StringComparison.OrdinalIgnoreCase)
            || alias.Equals(GlobalFolderRoot.ModelTypeAlias, StringComparison.OrdinalIgnoreCase)
            || node.Parent() is null
            || node.Parent()?.ContentType?.Alias?.Equals(GlobalFolderSites.ModelTypeAlias, StringComparison.OrdinalIgnoreCase) == true)
        {
            foreach (var child in node.Children() ?? [])
            {
                CollectCategoryItems(child, culture, result);
            }
        }
    }
}
