using System.Collections.Concurrent;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
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
    IVariationContextAccessor variationContextAccessor,
    IShortStringHelper shortStringHelper,
    HiddenContainerAliases hiddenContainerAliases)
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
    /// Uses Umbraco's <see cref="IShortStringHelper.CleanStringForUrlSegment"/> to ensure
    /// the segment is a valid URL-safe slug (handles spaces, diacritics, slashes, etc.).
    /// </summary>
    /// <param name="categoryItem">The category item whose URL segment is resolved.</param>
    /// <param name="culture">The culture used to resolve the URL segment, or the current variation culture when omitted.</param>
    /// <summary>
    /// Resolves the URL segment for a category item.
    /// </summary>
    /// <param name="categoryItem">The category item whose URL segment is resolved.</param>
    /// <param name="culture">The culture used to resolve the generated segment.</param>
    /// <returns>The sanitized custom segment or generated segment, or <c>null</c> when the category item or segment is invalid.</returns>
    public string? GetCategorySegment(IPublishedContent? categoryItem, string? culture)
    {
        if (categoryItem is null || categoryItem.Key == Guid.Empty)
        {
            return null;
        }

        var customSegment = categoryItem.Value<string>(CategoryUrlConstants.UrlSegmentAlias);
        if (!string.IsNullOrWhiteSpace(customSegment))
        {
            // Sanitize editor input through Umbraco's standard URL segment cleaner
            // to handle spaces, Vietnamese diacritics, slashes, and other invalid chars.
            var cleaned = shortStringHelper.CleanStringForUrlSegment(customSegment);
            return string.IsNullOrEmpty(cleaned) ? null : cleaned;
        }

        return documentUrlService.GetUrlSegment(
            categoryItem.Key,
            culture ?? variationContextAccessor.VariationContext?.Culture ?? string.Empty,
            false);
    }

    // Cache: (domainRootKey, culture) → category slugs dictionary.
    // Invalidated via InvalidateCache() which should be called on content publish notifications.
    private readonly ConcurrentDictionary<string, Dictionary<string, Guid>> _slugCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Invalidates the cached category slugs. Call this when category content is published.
    /// <summary>
/// Clears all cached category slug mappings.
/// </summary>
    public void InvalidateCache() => _slugCache.Clear();

    /// <summary>
    /// Gets all visible category slugs under a given site root.
    /// <summary>
    /// Collects visible category URL segments beneath a site root.
    /// </summary>
    /// <param name="culture">The culture used to resolve category URL segments.</param>
    /// <param name="siteRoot">The root content node from which to collect categories.</param>
    /// <returns>
    /// A case-insensitive mapping of category URL segments to category keys.
    /// <summary>
    /// Retrieves the visible category URL slugs for a site root.
    /// </summary>
    /// <param name="culture">The culture used to resolve category URLs.</param>
    /// <param name="siteRoot">The site root whose visible category slugs are retrieved.</param>
    /// <returns>
    /// A case-insensitive mapping of category URL slugs to category keys, or an empty mapping when no site root is supplied.
    /// </returns>
    public Dictionary<string, Guid> GetAllVisibleCategorySlugs(string? culture, IPublishedContent? siteRoot = null)
    {
        if (siteRoot is null)
        {
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        }

        return GetAllVisibleCategorySlugs(culture, siteRoot.Key);
    }

    /// <summary>
    /// Overload that accepts a domain root key. Results are cached per (domainRootKey, culture).
    /// </summary>
    /// <param name="culture">The culture used to resolve category URL segments.</param>
    /// <param name="domainRootKey">The key of the domain root content item.</param>
    /// <summary>
    /// Gets the visible category URL slugs for a domain root and culture.
    /// </summary>
    /// <param name="culture">The culture used to resolve category URLs.</param>
    /// <param name="domainRootKey">The key of the domain root content node.</param>
    /// <returns>A case-insensitive mapping of category URL slugs to category keys.</returns>
    public Dictionary<string, Guid> GetAllVisibleCategorySlugs(string? culture, Guid domainRootKey)
    {
        var cacheKey = $"{domainRootKey}|{culture ?? string.Empty}";

        if (_slugCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

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

        _slugCache.TryAdd(cacheKey, result);
        return result;
    }

    /// <summary>
    /// Gets the category landing URL and category name for a given page.
    /// Builds the URL from the non-transparent ancestor path (consistent with
    /// <see cref="CategoryUrlProvider"/> and <see cref="CategoryLandingContentFinder"/>).
    /// </summary>
    /// <param name="currentNode">The content page for which to build the category URL.</param>
    /// <param name="currentCat">The category to use, or the page's first visible category when omitted.</param>
    /// <summary>
    /// Builds the landing URL and display name for a page category.
    /// </summary>
    /// <param name="currentNode">The page for which to build the category landing URL.</param>
    /// <param name="currentCat">The category to use; when omitted, the page's first visible category is used.</param>
    /// <returns>A tuple containing the category URL and display name. The URL is empty when it cannot be built, while the name may still be available.</returns>
    public (string Url, string Name) GetUrlNameCategory(IPublishedContent currentNode, IPublishedContent? currentCat = null)
    {
        if (currentNode?.ContentType?.Alias is null)
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

        // Build the base URL without category and page segment, matching CategoryUrlProvider's logic:
        // Find the first non-transparent ancestor as the basis.
        var basis = currentNode.Ancestors().FirstOrDefault(a => !hiddenContainerAliases.IsTransparent(a));
        if (basis is null)
        {
            return (string.Empty, categoryCrumbName);
        }

        var resolvedCulture = variationContextAccessor.VariationContext?.Culture ?? string.Empty;

        // Get the base path from the non-transparent ancestor
        var basePath = basis.Url(mode: UrlMode.Relative)?.TrimEnd('/') ?? "";
        if (string.IsNullOrEmpty(basePath) || basePath == "#")
        {
            return (string.Empty, categoryCrumbName);
        }

        // Add intermediate non-transparent ancestors between basis and currentNode (excluding currentNode itself)
        var intermediates = currentNode.AncestorsOrSelf()
            .TakeWhile(a => a.Key != basis.Key)
            .Where(a => !hiddenContainerAliases.IsTransparent(a) && a.Key != currentNode.Key)
            .Reverse()
            .Select(a => documentUrlService.GetUrlSegment(a.Key, resolvedCulture, false))
            .Where(s => !string.IsNullOrEmpty(s));

        foreach (var seg in intermediates)
        {
            basePath += "/" + seg;
        }

        // Category landing URL = basePath + categorySegment + trailing slash
        var categoryCrumbUrl = $"{basePath}/{catSegment}/";
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
