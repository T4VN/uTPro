using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Outbound URL provider: inserts the first visible category's URL segment into the page URL.
/// </summary>
public sealed class CategoryUrlProvider(
    CategoryUrlService categoryUrlService,
    HiddenContainerAliases hidden,
    IPublishedUrlProvider publishedUrlProvider,
    IVariationContextAccessor variationContextAccessor,
    IDocumentUrlService documentUrlService) : IUrlProvider
{
    public string Alias => "uTProCategoryUrlProvider";

    /// <summary>
    /// Builds a URL that includes the content's first visible category.
    /// </summary>
    /// <param name="culture">The culture used to resolve the category and content URL segments.</param>
    /// <returns>The category-prefixed URL, or <c>null</c> when the content cannot produce one.</returns>
    public UrlInfo? GetUrl(IPublishedContent content, UrlMode mode, string? culture, Uri current)
    {
        if (hidden.IsTransparent(content))
        {
            return null;
        }

        if (!content.HasProperty(CategoryUrlConstants.PageCategoriesAlias))
        {
            return null;
        }

        var visibleCategories = CategoryUrlService.GetVisibleCategories(content);
        if (visibleCategories.Count == 0)
        {
            return null;
        }

        var firstCategory = visibleCategories[0];
        var categorySegment = categoryUrlService.GetCategorySegment(firstCategory, culture);
        if (string.IsNullOrEmpty(categorySegment))
        {
            return null;
        }

        return BuildCategoryUrl(content, categorySegment, mode, culture);
    }

    /// <summary>
/// Gets alternate URLs for the specified content.
/// </summary>
/// <param name="id">The content identifier.</param>
/// <param name="current">The current request URI.</param>
/// <returns>An empty collection.</returns>
public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current) => [];

    /// <summary>
        /// Provides no preview URL for the specified content.
        /// </summary>
        /// <param name="content">The content for which to provide a preview URL.</param>
        /// <param name="culture">The requested culture.</param>
        /// <param name="segment">The requested URL segment.</param>
        /// <returns><c>null</c>.</returns>
        public Task<UrlInfo?> GetPreviewUrlAsync(Umbraco.Cms.Core.Models.IContent content, string? culture, string? segment)
        => Task.FromResult<UrlInfo?>(null);

    /// <summary>
    /// Builds a category-based URL for the specified content.
    /// </summary>
    /// <param name="categorySegment">The URL segment for the content's category.</param>
    /// <returns>The constructed category URL, or null when a valid URL cannot be resolved.</returns>
    private UrlInfo? BuildCategoryUrl(IPublishedContent content, string categorySegment, UrlMode mode, string? culture)
    {
        var resolvedCulture = culture ?? variationContextAccessor.VariationContext?.Culture ?? string.Empty;
        var pageSegment = documentUrlService.GetUrlSegment(content.Key, resolvedCulture, false);
        if (string.IsNullOrEmpty(pageSegment))
        {
            return null;
        }

        var basis = content.Ancestors().FirstOrDefault(a => !hidden.IsTransparent(a));
        if (basis is null)
        {
            return null;
        }

        var baseUrl = publishedUrlProvider.GetUrl(basis.Key, mode, culture);
        if (string.IsNullOrEmpty(baseUrl) || baseUrl == "#")
        {
            return null;
        }

        var trailingSlash = baseUrl.EndsWith('/');
        var path = baseUrl.TrimEnd('/');

        var intermediates = content.AncestorsOrSelf()
            .TakeWhile(a => a.Key != basis.Key)
            .Where(a => !hidden.IsTransparent(a) && a.Key != content.Key)
            .Reverse()
            .Select(a => documentUrlService.GetUrlSegment(a.Key, resolvedCulture, false))
            .Where(s => !string.IsNullOrEmpty(s));

        foreach (var seg in intermediates)
        {
            path += "/" + seg;
        }

        path += "/" + categorySegment;
        path += "/" + pageSegment;

        if (trailingSlash)
        {
            path += "/";
        }

        return UrlInfo.AsUrl(path, Alias, culture);
    }
}
