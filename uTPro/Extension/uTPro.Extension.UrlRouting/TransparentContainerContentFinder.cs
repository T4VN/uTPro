using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Resolves an incoming "clean" URL (with container segments omitted) back to the real content node.
/// </summary>
public sealed class TransparentContainerContentFinder : IContentFinder
{
    private readonly HiddenContainerAliases _hidden;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly IVariationContextAccessor _variationContextAccessor;
    private readonly IDocumentNavigationQueryService _navigationQueryService;
    private readonly IPublishedContentStatusFilteringService _publishedStatusFilteringService;
    private readonly IDocumentUrlService _documentUrlService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransparentContainerContentFinder"/> class.
    /// </summary>
    /// <param name="hidden">The configured transparent container aliases.</param>
    public TransparentContainerContentFinder(
        HiddenContainerAliases hidden,
        IUmbracoContextAccessor umbracoContextAccessor,
        IVariationContextAccessor variationContextAccessor,
        IDocumentNavigationQueryService navigationQueryService,
        IPublishedContentStatusFilteringService publishedStatusFilteringService,
        IDocumentUrlService documentUrlService)
    {
        _hidden = hidden;
        _umbracoContextAccessor = umbracoContextAccessor;
        _variationContextAccessor = variationContextAccessor;
        _navigationQueryService = navigationQueryService;
        _publishedStatusFilteringService = publishedStatusFilteringService;
        _documentUrlService = documentUrlService;
    }

    /// <summary>
            /// Gets the URL segment for a content node in the specified culture.
            /// </summary>
            /// <param name="node">The content node whose URL segment is retrieved.</param>
            /// <param name="culture">The requested culture, or the current variation culture when omitted.</param>
            /// <returns>The node's URL segment, or <c>null</c> when no segment is available.</returns>
            private string? GetSegment(IPublishedContent node, string? culture)
        => _documentUrlService.GetUrlSegment(
            node.Key,
            culture ?? _variationContextAccessor.VariationContext?.Culture ?? string.Empty,
            false);

    /// <summary>
    /// Resolves a clean URL to published content by bypassing configured transparent containers.
    /// </summary>
    /// <param name="request">The published request to resolve.</param>
    /// <summary>
    /// Resolves content for a URL that bypasses one or more transparent containers.
    /// </summary>
    /// <param name="request">The published request to resolve.</param>
    /// <returns><c>true</c> if content is resolved after bypassing a transparent container; <c>false</c> otherwise.</returns>
    public Task<bool> TryFindContent(IPublishedRequestBuilder request)
    {
        if (request.PublishedContent is not null)
        {
            return Task.FromResult(false);
        }

        if (request.Domain is null)
        {
            return Task.FromResult(false);
        }

        if (!_umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext)
            || umbracoContext.Content is null)
        {
            return Task.FromResult(false);
        }

        var root = umbracoContext.Content.GetById(request.Domain.ContentId);
        if (root is null)
        {
            return Task.FromResult(false);
        }

        // Split path first, then decode each segment individually.
        // This prevents encoded slashes (%2F) from being decoded into '/' and creating extra segments.
        var rawPath = request.Uri.AbsolutePath.Trim('/');
        var domainPath = request.Domain.Uri?.AbsolutePath.Trim('/') ?? string.Empty;
        rawPath = RequestPathHelper.StripDomainPrefix(rawPath, domainPath);

        if (rawPath.Length == 0)
        {
            return Task.FromResult(false);
        }

        var segments = rawPath.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();
        var culture = request.Culture;

        var current = root;
        var skippedContainer = false;

        foreach (var segment in segments)
        {
            var match = FindChild(current, segment, culture, ref skippedContainer);
            if (match is null)
            {
                return Task.FromResult(false);
            }

            current = match;
        }

        if (!skippedContainer)
        {
            return Task.FromResult(false);
        }

        request.SetPublishedContent(current);
        return Task.FromResult(true);
    }

    /// <summary>
    /// Finds a child matching the specified URL segment, traversing transparent containers when necessary.
    /// </summary>
    /// <param name="parent">The content node whose descendants are searched.</param>
    /// <param name="segment">The URL segment to match.</param>
    /// <param name="culture">The culture used to resolve URL segments.</param>
    /// <param name="skippedContainer">Indicates whether a transparent container was bypassed.</param>
    /// <returns>The matching content node, or null when no match is found.</returns>
    private IPublishedContent? FindChild(
        IPublishedContent parent, string segment, string? culture, ref bool skippedContainer)
    {
        var children = parent.Children(_navigationQueryService, _publishedStatusFilteringService)
            ?? Enumerable.Empty<IPublishedContent>();

        foreach (var child in children)
        {
            if (_hidden.IsTransparent(child))
            {
                var inner = FindChild(child, segment, culture, ref skippedContainer);
                if (inner is not null)
                {
                    skippedContainer = true;
                    return inner;
                }

                continue;
            }

            if (string.Equals(
                    GetSegment(child, culture),
                    segment,
                    StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }
}
