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

    public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current) => [];

    public Task<UrlInfo?> GetPreviewUrlAsync(Umbraco.Cms.Core.Models.IContent content, string? culture, string? segment)
        => Task.FromResult<UrlInfo?>(null);

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
