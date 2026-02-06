using Umbraco.Cms.Core.Models.PublishedContent;
namespace uTPro.Extension
{
    public static class ContentExtensions
    {
        public static T? ValueInherited<T>(this IPublishedContent content, string alias)
        {
            var node = content.Inherited(alias);
            if (node == null)
                return default;

            return node.Value<T>(alias);
        }

        public static IPublishedContent? Inherited(this IPublishedContent content, string alias)
        {
            return content.AncestorsOrSelf().FirstOrDefault(x => x.HasValue(alias));
        }

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
    }
}
