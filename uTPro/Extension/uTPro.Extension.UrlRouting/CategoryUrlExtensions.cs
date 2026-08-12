using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Extension methods for getting category-based URLs programmatically.
/// </summary>
public static class CategoryUrlExtensions
{
    /// <summary>
    /// Gets the category-based URL for a page using a visible category.
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="IPublishedUrlProvider.GetUrl"/> which routes through
    /// <see cref="CategoryUrlProvider"/>, ensuring consistent URL generation that
    /// correctly handles transparent containers.
    /// When a specific <paramref name="categoryKey"/> is provided and doesn't match
    /// the first visible category, falls back to segment swapping on the resolved URL.
    /// </remarks>
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

        // When requesting the first category, GetUrl already goes through CategoryUrlProvider
        // which handles transparent containers correctly.
        var pageUrl = urlProvider.GetUrl(page.Key, UrlMode.Default, culture);
        if (string.IsNullOrEmpty(pageUrl) || pageUrl == "#")
        {
            return null;
        }

        // If the target is the first category, the URL from the provider is already correct.
        if (targetCategory.Key == visibleCategories[0].Key)
        {
            return pageUrl;
        }

        // For a non-default category, swap the category segment in the provider-generated URL.
        var defaultSegment = categoryUrlService.GetCategorySegment(visibleCategories[0], culture);
        var targetSegment = categoryUrlService.GetCategorySegment(targetCategory, culture);
        if (string.IsNullOrEmpty(defaultSegment) || string.IsNullOrEmpty(targetSegment))
        {
            return null;
        }

        return ReplaceCategorySegment(pageUrl, defaultSegment, targetSegment);
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

        // Get the URL from the provider (uses CategoryUrlProvider, handles containers correctly)
        var pageUrl = urlProvider.GetUrl(page.Key, UrlMode.Default, culture);
        if (string.IsNullOrEmpty(pageUrl) || pageUrl == "#")
        {
            return [];
        }

        // The provider-generated URL already contains the first category segment
        var defaultCategory = visibleCategories[0];
        var defaultSegment = categoryUrlService.GetCategorySegment(defaultCategory, culture);
        if (string.IsNullOrEmpty(defaultSegment))
        {
            return [];
        }

        var result = new List<(Guid, string)>();

        // First category is already in the provider URL
        result.Add((defaultCategory.Key, pageUrl));

        // For remaining categories, swap the segment
        for (int i = 1; i < visibleCategories.Count; i++)
        {
            var cat = visibleCategories[i];
            var segment = categoryUrlService.GetCategorySegment(cat, culture);
            if (string.IsNullOrEmpty(segment))
            {
                continue;
            }

            var url = ReplaceCategorySegment(pageUrl, defaultSegment, segment);
            if (url is not null)
            {
                result.Add((cat.Key, url));
            }
        }

        return result;
    }

    /// <summary>
    /// Replaces a category segment within a URL (handles both relative and absolute URLs safely).
    /// </summary>
    private static string? ReplaceCategorySegment(string url, string oldSegment, string newSegment)
    {
        // Extract just the path portion, handling both absolute and relative URLs
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            var path = absoluteUri.AbsolutePath;
            var newPath = ReplaceSegmentInPath(path, oldSegment, newSegment);
            if (newPath is null) return null;
            return absoluteUri.GetLeftPart(UriPartial.Authority) + newPath;
        }

        // Relative URL — operate on the string directly as a path
        return ReplaceSegmentInPath(url, oldSegment, newSegment);
    }

    /// <summary>
    /// Replaces the penultimate segment (category position) in a path string.
    /// </summary>
    private static string? ReplaceSegmentInPath(string path, string oldSegment, string newSegment)
    {
        var trailingSlash = path.EndsWith('/');
        var trimmed = path.TrimEnd('/');

        var segments = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return null;
        }

        // Category is always at penultimate position (just before page segment)
        var categoryIndex = segments.Length - 2;
        if (!segments[categoryIndex].Equals(oldSegment, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        segments[categoryIndex] = newSegment;

        var result = "/" + string.Join("/", segments);
        if (trailingSlash)
        {
            result += "/";
        }

        return result;
    }
}
