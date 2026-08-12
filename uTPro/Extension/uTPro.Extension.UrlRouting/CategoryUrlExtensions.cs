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
    /// <summary>
    /// Builds a URL for the page using a visible category segment.
    /// </summary>
    /// <param name="culture">The culture used to resolve the category segment and page URL.</param>
    /// <param name="categoryKey">The key of the category to use; when omitted, the first visible category is selected.</param>
    /// <returns>The category URL, or <see langword="null"/> when no suitable category or page URL is available.</returns>
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
    /// <summary>
    /// Generates URLs for all visible categories associated with a page.
    /// </summary>
    /// <param name="culture">The culture used to resolve the page URL and category segments.</param>
    /// <returns>
    /// The category keys and corresponding URLs for categories with valid segments, or an empty list when no valid URLs can be generated.
    /// </returns>
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

    /// <summary>
    /// Inserts a category segment before the page segment in a URL.
    /// </summary>
    /// <param name="url">The URL containing the page segment.</param>
    /// <param name="categorySegment">The category segment to insert.</param>
    /// <returns>The URL with the category segment inserted, or <see langword="null"/> if the URL has no slash.</returns>
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
