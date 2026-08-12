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
    /// <summary>
    /// Traverses the content tree using the specified URL segments.
    /// </summary>
    /// <param name="root">The content item at which traversal begins.</param>
    /// <param name="segments">The URL segments to resolve in order.</param>
    /// <param name="culture">The culture used to resolve URL segments, or null for the current culture.</param>
    /// <returns>The content item resolved by all segments, or null if any segment cannot be resolved.</returns>
    public IPublishedContent? WalkTree(IPublishedContent root, string[] segments, string? culture)
    {
        return segments.Aggregate(
            (IPublishedContent?)root,
            (current, segment) => current is null ? null : FindChild(current, segment, culture));
    }

    /// <summary>
    /// Finds a direct child matching the given URL segment, stepping through transparent containers.
    /// <summary>
    /// Finds a published descendant matching the specified URL segment, traversing transparent containers.
    /// </summary>
    /// <param name="parent">The content item whose descendants are searched.</param>
    /// <param name="segment">The URL segment to match.</param>
    /// <param name="culture">The culture used to generate URL segments, or the current variation culture when omitted.</param>
    /// <returns>The matching content item, or <c>null</c> when no descendant matches.</returns>
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
