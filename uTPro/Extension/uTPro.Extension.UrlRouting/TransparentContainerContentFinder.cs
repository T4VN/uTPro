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

    private string? GetSegment(IPublishedContent node, string? culture)
        => _documentUrlService.GetUrlSegment(
            node.Key,
            culture ?? _variationContextAccessor.VariationContext?.Culture ?? string.Empty,
            false);

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
