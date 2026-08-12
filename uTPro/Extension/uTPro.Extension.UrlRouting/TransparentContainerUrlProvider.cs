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

    /// <summary>
    /// Initializes a URL provider that omits configured transparent container nodes from public URLs.
    /// </summary>
    /// <param name="hidden">The configured transparent container aliases.</param>
    /// <param name="variationContextAccessor">Provides the current variation context.</param>
    /// <param name="publishedUrlProvider">Generates published content URLs.</param>
    /// <param name="documentUrlService">Resolves document URL segments.</param>
    /// <param name="categoryUrlService">Handles category URL generation.</param>
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

    /// <summary>
            /// Gets the URL segment for a content node in the specified or current culture.
            /// </summary>
            /// <param name="node">The content node whose URL segment is requested.</param>
            /// <param name="culture">The culture used to resolve the URL segment, or <see langword="null"/> to use the current variation culture.</param>
            /// <returns>The node's URL segment, or <see langword="null"/> if none is available.</returns>
            private string? GetSegment(IPublishedContent node, string? culture)
        => _documentUrlService.GetUrlSegment(
            node.Key,
            culture ?? _variationContextAccessor.VariationContext?.Culture ?? string.Empty,
            false);

    /// <summary>
    /// Builds a public URL that omits transparent container nodes.
    /// </summary>
    /// <param name="content">The content item for which to build the URL.</param>
    /// <param name="mode">The URL generation mode.</param>
    /// <param name="culture">The culture used to resolve the URL.</param>
    /// <param name="current">The current request URI.</param>
    /// <returns>The generated URL, or null when this provider does not handle the content.</returns>
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

    /// <summary>
/// Provides no alternative URLs for the specified content identifier.
/// </summary>
/// <param name="id">The content identifier.</param>
/// <param name="current">The current request URI.</param>
/// <returns>An empty collection.</returns>
public IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current) => [];

    /// <summary>
        /// Indicates that no preview URL is available for the content.
        /// </summary>
        /// <param name="content">The content for which to retrieve a preview URL.</param>
        /// <param name="culture">The requested culture.</param>
        /// <param name="segment">The requested URL segment.</param>
        /// <returns><c>null</c>, indicating that no preview URL is available.</returns>
        public Task<UrlInfo?> GetPreviewUrlAsync(Umbraco.Cms.Core.Models.IContent content, string? culture, string? segment)
        => Task.FromResult<UrlInfo?>(null);
}
