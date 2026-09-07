using Microsoft.AspNetCore.Http;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace uTPro.Extension
{
    public static class ContentExtensions
    {
        // Per-request cache to avoid walking AncestorsOrSelf() multiple times for
        // the same (contentId, alias). HasValue() traversal is cheap per-call but
        // templates call ValueInherited many times per page (topComponent,
        // bottomComponent, site-level toggles) so the repeated walks add up.
        private static readonly object _cacheKey = new();

        /// <summary>
        /// Retrieves a strongly-typed value for the specified property alias by walking up the content
        /// tree (self and ancestors) until a node with that property is found, then returns the value
        /// cast to type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The expected type of the property value.</typeparam>
        /// <param name="content">The content node to start searching from.</param>
        /// <param name="alias">The property alias to search for.</param>
        /// <returns>The inherited property value, or the default value of <typeparamref name="T"/> if not found.</returns>
        public static T? ValueInherited<T>(this IPublishedContent content, string alias)
        {
            var node = content.Inherited(alias);
            return node == null ? default : node.Value<T>(alias);
        }

        /// <summary>
        /// Finds the nearest ancestor (or self) that has a value for the specified property alias.
        /// Results are cached per-request to avoid repeated tree traversals for the same content and alias.
        /// </summary>
        /// <param name="content">The content node to start searching from.</param>
        /// <param name="alias">The property alias to search for.</param>
        /// <returns>The nearest <see cref="IPublishedContent"/> node that has the property, or null if not found.</returns>
        public static IPublishedContent? Inherited(this IPublishedContent content, string alias)
        {
            var cache = TryGetCache();
            if (cache != null)
            {
                var key = (content.Id, alias);
                if (cache.TryGetValue(key, out var cached))
                {
                    return cached;
                }

                var found = ResolveInherited(content, alias);
                cache[key] = found;
                return found;
            }

            return ResolveInherited(content, alias);
        }

        /// <summary>
        /// Searches the content tree (self and ancestors) for the first node that has a value for any
        /// of the specified property aliases, returning both the matched alias and the node containing it.
        /// </summary>
        /// <param name="content">The content node to start searching from.</param>
        /// <param name="alias">One or more property aliases to search for, checked in order.</param>
        /// <returns>
        /// A tuple containing the matched alias and the <see cref="IPublishedContent"/> node that has it,
        /// or (null, null) if none of the aliases are found.
        /// </returns>
        public static (string? alias, IPublishedContent? value) Inherited(this IPublishedContent content, params string[] alias)
        {
            foreach (var node in content.AncestorsOrSelf())
            {
                foreach (var a in alias)
                {
                    if (node.HasValue(a))
                        return (a, node.Value<IPublishedContent>(a));
                }
            }

            return (null, null);
        }

        private static IPublishedContent? ResolveInherited(IPublishedContent content, string alias)
        {
            // AncestorsOrSelf enumerates self first then parents - checking HasValue
            // short-circuits on the first hit.
            foreach (var node in content.AncestorsOrSelf())
            {
                if (node.HasValue(alias))
                {
                    return node;
                }
            }
            return null;
        }

        private static Dictionary<(int id, string alias), IPublishedContent?>? TryGetCache()
        {
            var http = HttpContextStatic.Accessor?.HttpContext;
            if (http == null) return null;

            if (http.Items.TryGetValue(_cacheKey, out var existing)
                && existing is Dictionary<(int, string), IPublishedContent?> dict)
            {
                return dict;
            }

            var created = new Dictionary<(int, string), IPublishedContent?>();
            http.Items[_cacheKey] = created;
            return created;
        }
    }
}
