using System.Net.NetworkInformation;
using Umbraco.Cms.Core.Models.PublishedContent;
using System.Linq;

namespace uTPro.Extension
{
    public static class ContentExtensions
    {
        public static T? GetInherited<T>(this IPublishedContent content, string alias)
        {
            var node = content.AncestorsOrSelf()
                .FirstOrDefault(x => x.HasValue(alias));

            if (node == null)
                return default;

            return node.Value<T>(alias);
        }
    }

}
