using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Makes "container" nodes transparent in the public URL: a child at
/// <c>/huong-dan/pages/hd1</c> in the content tree is served at <c>/huong-dan/hd1</c>.
/// </summary>
public sealed class TransparentContainerUrlProvider : IUrlProvider
{
    private readonly HiddenContainerAliases _hidden;
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly IPublishedUrlProvider _publishedUrlProvider;
    private readonly IDocumentUrlService _documentUrlService;
    private readonly CategoryUrlService _categoryUrlService;

    public TransparentContainerUrlProvider(
        HiddenContainerAliases hidden,
        IVariationContextAccessor variationContextAccessor,
        IPublishedUrlProvider publishedUrlProvider,
        IDocumentUrlService documentUrlService,
        CategoryUrlService categoryUrlService)
    {
        _hidden = hidden;
        _variationContextAccessor = variationContextAccessor;
        _publishedUrlProvider = publishedUrlProvider;
        _documentUrlService = documentUrlService;
        _categoryUrlService = categoryUrlService;
    }

    public string Alias => "uTProTransparentContainerUrlProvider";

    private string? GetSegment(IPublishedContent node, string? culture)
        => _documentUrlService.GetUrlSegment(
            node.Key,
            culture ?? _variationContextAccessor.VariationContext?.Culture ?? string.Empty,
            false);

    public UrlInfo? GetUrl(IPublishedContent content, UrlMode mode, string? culture, Uri current)
    {
        if (_hidden.IsTransparent(content))
        {
            return null;
        }

        // If the page has visible categories, yield to CategoryUrlProvider.
        if (content.HasProperty(CategoryUrlConstants.PageCategoriesAlias)
            && CategoryUrlService.GetVisibleCategories(content).Count > 0)
        {
            return null;
        }

        if (!content.Ancestors().Any(a => _hidden.IsTransparent(a)))
        {
            return null;
        }

        var basis = content.Ancestors().FirstOrDefault(a => !_hidden.IsTransparent(a));
        if (basis is null)
        {
            return null;
        }

        var baseUrl = _publishedUrlProvider.GetUrl(basis.Key, mode, culture);
        if (string.IsNullOrEmpty(baseUrl) || baseUrl == "#")
        {
            return null;
        }

        var trailingSlash = baseUrl.EndsWith('/');
        var path = baseUrl.TrimEnd('/');

        var tail = content.AncestorsOrSelf()
            .TakeWhile(a => a.Key != basis.Key)
            .Where(a => !_hidden.IsTransparent(a))
            .Reverse()
            .Select(a => GetSegment(a, culture))
            .Where(s => !string.IsNullOrEmpty(s));

        foreach (var segment in tail)
        {
            path += "/" + segment;
        }

        if (trailingSlash)
        {
            path += "/";
        }

        return UrlInfo.AsUrl(path, Alias, culture);
    }

    public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current) => [];

    public Task<UrlInfo?> GetPreviewUrlAsync(Umbraco.Cms.Core.Models.IContent content, string? culture, string? segment)
        => Task.FromResult<UrlInfo?>(null);
}
