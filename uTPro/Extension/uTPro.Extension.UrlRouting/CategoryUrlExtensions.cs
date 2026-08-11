using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Extension methods for getting category-based URLs programmatically.
/// </summary>
public static class CategoryUrlExtensions
{
    /// <summary>
    /// Gets the category-based URL for a page using the first visible category.
    /// </summary>
    public static string? GetCategoryUrl(
        this IPublishedContent page,
        CategoryUrlService categoryUrlService,
        IPublishedUrlProvider urlProvider,
        string? culture = null,
        Guid? categoryKey = null)
    {
        var visibleCategories = CategoryUrlService.GetVisibleCategories(page);
        if (visibleCategories.Count == 0)
        {
            return null;
        }

        IPublishedContent? targetCategory;

        if (categoryKey.HasValue)
        {
            targetCategory = visibleCategories.FirstOrDefault(c => c.Key == categoryKey.Value);
            if (targetCategory is null)
            {
                return null;
            }
        }
        else
        {
            targetCategory = visibleCategories[0];
        }

        var categorySegment = categoryUrlService.GetCategorySegment(targetCategory, culture);
        if (string.IsNullOrEmpty(categorySegment))
        {
            return null;
        }

        var baseUrl = urlProvider.GetUrl(page.Key, UrlMode.Default, culture);
        if (string.IsNullOrEmpty(baseUrl) || baseUrl == "#")
        {
            return null;
        }

        return InsertCategorySegment(baseUrl, categorySegment);
    }

    /// <summary>
    /// Gets all category-based URLs for a page (one per visible category).
    /// </summary>
    public static IReadOnlyList<(Guid CategoryKey, string Url)> GetAllCategoryUrls(
        this IPublishedContent page,
        CategoryUrlService categoryUrlService,
        IPublishedUrlProvider urlProvider,
        string? culture = null)
    {
        var visibleCategories = CategoryUrlService.GetVisibleCategories(page);
        if (visibleCategories.Count == 0)
        {
            return [];
        }

        var baseUrl = urlProvider.GetUrl(page.Key, UrlMode.Default, culture);
        if (string.IsNullOrEmpty(baseUrl) || baseUrl == "#")
        {
            return [];
        }

        var result = new List<(Guid, string)>();

        foreach (var cat in visibleCategories)
        {
            var segment = categoryUrlService.GetCategorySegment(cat, culture);
            if (string.IsNullOrEmpty(segment))
            {
                continue;
            }

            var url = InsertCategorySegment(baseUrl, segment);
            if (url is not null)
            {
                result.Add((cat.Key, url));
            }
        }

        return result;
    }

    private static string? InsertCategorySegment(string url, string categorySegment)
    {
        var trailingSlash = url.EndsWith('/');
        var path = url.TrimEnd('/');

        var lastSlash = path.LastIndexOf('/');
        if (lastSlash < 0)
        {
            return null;
        }

        var beforePage = path[..lastSlash];
        var pageSegment = path[(lastSlash + 1)..];

        var result = $"{beforePage}/{categorySegment}/{pageSegment}";

        if (trailingSlash)
        {
            result += "/";
        }

        return result;
    }
}
