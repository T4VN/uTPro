using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Extensions;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Shared helper for walking the Umbraco content tree through transparent containers.
/// Used by both <see cref="CategoryUrlContentFinder"/> and <see cref="CategoryLandingContentFinder"/>.
/// </summary>
public sealed class ContentTreeWalker(
    HiddenContainerAliases hidden,
    IVariationContextAccessor variationContextAccessor,
    IDocumentNavigationQueryService navigationQueryService,
    IPublishedContentStatusFilteringService publishedStatusFilteringService,
    IDocumentUrlService documentUrlService)
{
    /// <summary>
    /// Walks the content tree from <paramref name="root"/> through the given segments,
    /// stepping through transparent containers.
    /// </summary>
    public IPublishedContent? WalkTree(IPublishedContent root, string[] segments, string? culture)
    {
        return segments.Aggregate(
            (IPublishedContent?)root,
            (current, segment) => current is null ? null : FindChild(current, segment, culture));
    }

    /// <summary>
    /// Finds a direct child matching the given URL segment, stepping through transparent containers.
    /// </summary>
    public IPublishedContent? FindChild(IPublishedContent parent, string segment, string? culture)
    {
        var children = parent.Children(navigationQueryService, publishedStatusFilteringService)
            ?? [];

        foreach (var child in children)
        {
            if (hidden.IsTransparent(child))
            {
                var inner = FindChild(child, segment, culture);
                if (inner is not null)
                {
                    return inner;
                }

                continue;
            }

            var childSegment = documentUrlService.GetUrlSegment(
                child.Key,
                culture ?? variationContextAccessor.VariationContext?.Culture ?? string.Empty,
                false);

            if (string.Equals(childSegment, segment, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }
}
