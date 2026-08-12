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
    /// <summary>
    /// Gets the categories configured to appear in the page URL.
    /// </summary>
    /// <param name="page">The page whose category selections are evaluated.</param>
    /// <returns>The visible categories in picker order.</returns>
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
    /// <summary>
    /// Resolves the URL segment for a category item.
    /// </summary>
    /// <param name="categoryItem">The category item whose URL segment is resolved.</param>
    /// <param name="culture">The culture used to resolve the URL segment, or the current variation culture when omitted.</param>
    /// <returns>The normalized custom or generated URL segment, or <c>null</c> when the category item is unavailable or invalid.</returns>
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
    /// <summary>
    /// Collects visible category URL segments beneath a site root.
    /// </summary>
    /// <param name="culture">The culture used to resolve category URL segments.</param>
    /// <param name="siteRoot">The root content node from which to collect categories.</param>
    /// <returns>
    /// A case-insensitive mapping of category URL segments to category keys.
    /// </returns>
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
    /// <summary>
    /// Collects visible category URL slugs from the domain root or its nearest ancestor containing categories.
    /// </summary>
    /// <param name="culture">The culture used to resolve category URL segments.</param>
    /// <param name="domainRootKey">The key of the domain root content item.</param>
    /// <returns>A case-insensitive mapping of category URL slugs to category keys.</returns>
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
    /// <summary>
    /// Builds the category landing URL and display name for a content page.
    /// </summary>
    /// <param name="currentNode">The content page for which to build the category URL.</param>
    /// <param name="currentCat">The category to use, or the page's first visible category when omitted.</param>
    /// <returns>A tuple containing the category URL and display name, or empty values when the required content is unavailable.</returns>
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
    /// <summary>
    /// Determines whether a page contains a category with the specified key.
    /// </summary>
    /// <param name="categoryKey">The key of the category to find.</param>
    /// <returns><c>true</c> if the page contains the specified category; otherwise, <c>false</c>.</returns>
    public static bool PageHasCategory(IPublishedContent page, Guid categoryKey)
    {
        var categories = page.Value<IEnumerable<IPublishedContent>>(CategoryUrlConstants.PageCategoriesAlias);
        return categories?.Any(c => c is not null && c.Key == categoryKey) == true;
    }

    /// <summary>
    /// Collects visible category items beneath eligible content nodes and adds their URL segments and keys to the result.
    /// </summary>
    /// <param name="node">The content node from which to begin traversal.</param>
    /// <param name="culture">The culture used to resolve category URL segments.</param>
    /// <param name="result">The dictionary to populate with category URL segments and keys.</param>
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
