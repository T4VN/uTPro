using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Invalidates the <see cref="CategoryUrlService"/> slug cache whenever content is published,
/// ensuring category URL changes are reflected immediately.
/// </summary>
internal sealed class CategorySlugCacheInvalidator(CategoryUrlService categoryUrlService)
    : INotificationHandler<ContentPublishedNotification>
{
    /// <summary>
    /// Clears the category URL slug cache after content is published.
    /// </summary>
    /// <param name="notification">The content publication notification.</param>
    public void Handle(ContentPublishedNotification notification)
    {
        // Any publish could potentially affect category items or their parents,
        // so clear the entire slug cache. This is cheap (just clears a ConcurrentDictionary)
        // and the cache rebuilds lazily on next request.
        categoryUrlService.InvalidateCache();
    }
}
