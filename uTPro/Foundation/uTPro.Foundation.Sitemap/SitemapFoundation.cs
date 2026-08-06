using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Xml.Linq;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;
using uTPro.Extension;
using uTPro.Extension.CurrentSite;

namespace uTPro.Foundation.Sitemap
{
    internal class SitemapFoundation : ISitemapFoundation
    {
        private const string CacheKeyPrefix = "uTPro:Sitemap:";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        private static readonly XNamespace SchemaSitemap = "http://www.sitemaps.org/schemas/sitemap/0.9";
        private static readonly XNamespace SchemaXhtml = "http://www.w3.org/1999/xhtml";
        private static readonly XNamespace SchemaXsd = "http://www.w3.org/2001/XMLSchema";
        private static readonly XNamespace SchemaXsi = "http://www.w3.org/2001/XMLSchema-instance";

        private sealed class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        private readonly ICurrentSiteExtension _currentSite;
        private readonly IMemoryCache _cache;

        public SitemapFoundation(ICurrentSiteExtension currentSite, IMemoryCache cache)
        {
            _currentSite = currentSite;
            _cache = cache;
        }

        public string Generate()
        {
            // Cache key includes the host so multi-site deployments get separate sitemaps.
            var cacheKey = CacheKeyPrefix + (_currentSite.GetItem().Root?.Name ?? "default");

            if (_cache.TryGetValue(cacheKey, out string? cached) && cached != null)
            {
                return cached;
            }

            var result = BuildSitemap();
            _cache.Set(cacheKey, result, CacheDuration);
            return result;
        }

        private string BuildSitemap()
        {
            XElement root = InitElementRoot();
            var nodes = GetListNodes();

            foreach (var dataNode in nodes)
            {
                string culture = dataNode.Culture;
                var node = dataNode.Content;
                if (node == null) continue;

                // Skip nodes that have no routable URL (e.g. container/organisational nodes such as
                // globalFolderPages). These sit inside the content tree below Home but resolve to "#"
                // via the URL providers, so they must never appear in the sitemap.
                var loc = _currentSite.GetUrlWithCulture(node, culture, mode: UrlMode.Absolute);
                if (string.IsNullOrEmpty(loc) || loc.Contains('#')) continue;

                XElement urlElement = new XElement(SchemaSitemap + "url");
                urlElement.Add(new XElement(SchemaSitemap + "loc", loc));
                AddLastMod(node, urlElement);
                AddChangeFrequency(node, culture, urlElement);
                AddPriority(node, culture, urlElement);
                AddXhtmlLinks(node, urlElement);

                root.Add(urlElement);
            }
            
            XDocument document = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
            return ConvertDocumentToString(document);
        }

        private void AddLastMod(IPublishedContent node, XElement element)
        {
            element.Add(new XElement(SchemaSitemap + "lastmod", node.UpdateDate.ToString("yyyy-MM-dd")));
        }

        private void AddChangeFrequency(IPublishedContent node, string culture, XElement element)
        {
            var changeFrequency = node.Value<string>(nameof(GlobalPageSitemapSetting.SitemapXmlChangeFrequency), culture);
            if (!string.IsNullOrWhiteSpace(changeFrequency))
            {
                element.Add(new XElement(SchemaSitemap + "changefreq", changeFrequency.ToLowerInvariant()));
            }
        }

        private void AddPriority(IPublishedContent node, string culture, XElement element)
        {
            var priority = node.Value<decimal>(nameof(GlobalPageSitemapSetting.SitemapXmlPriority), culture);
            if (priority > 0)
            {
                element.Add(new XElement(SchemaSitemap + "priority", priority));
            }
            else
            {
                // Auto-calculate based on depth (Google ignores priority but some crawlers use it)
                decimal autoPriority = Math.Max(0.1M, 1.1M - ((node.Level - 1) / 10M));
                element.Add(new XElement(SchemaSitemap + "priority", autoPriority));
            }
        }

        private void AddXhtmlLinks(IPublishedContent node, XElement element)
        {
            if (node.Cultures.Count <= 1) return;

            foreach (var itemCul in node.Cultures)
            {
                if (node.Value<bool>(nameof(GlobalPageSitemapSetting.SitemapHiddenSitemap), itemCul.Value.Culture))
                    continue;

                var href = _currentSite.GetUrlWithCulture(node, itemCul.Value.Culture, mode: UrlMode.Absolute);
                if (string.IsNullOrEmpty(href) || href.Contains('#')) continue;

                var elementCul = new XElement(SchemaXhtml + "link",
                    new XAttribute("rel", "alternate"),
                    new XAttribute("hreflang", itemCul.Value.Culture),
                    new XAttribute("href", href));
                element.Add(elementCul);
            }
        }

        private XElement InitElementRoot()
        {
            return new XElement(SchemaSitemap + "urlset",
                new XAttribute(XNamespace.Xmlns + "xhtml", SchemaXhtml),
                new XAttribute(XNamespace.Xmlns + "xsd", SchemaXsd),
                new XAttribute(XNamespace.Xmlns + "xsi", SchemaXsi));
        }

        private string ConvertDocumentToString(XDocument document)
        {
            var sw = new Utf8StringWriter();
            sw.WriteLine(document.Declaration?.ToString());
            sw.WriteLine(document.ToString());
            return sw.ToString();
        }

        private IEnumerable<SitemapEntry> GetListNodes()
        {
            var rootNode = _currentSite.GetItem().PageHome;
            if (rootNode == null) yield break;

            var cultures = _currentSite.GetCultures().ToList();

            foreach (var cultureInfo in cultures)
            {
                string path = string.Empty;
                var lstContent = rootNode.DescendantsOrSelf(cultureInfo.Culture)
                    .OrderBy(x => x.Path)
                    .ToList();

                foreach (var itemContent in lstContent)
                {
                    // Skip children of hidden-children nodes
                    if (!string.IsNullOrEmpty(path) && itemContent.Path.StartsWith(path, StringComparison.Ordinal))
                        continue;

                    if (!itemContent.Value<bool>(nameof(GlobalPageSitemapSetting.SitemapHiddenSitemap), cultureInfo.Culture))
                    {
                        yield return new SitemapEntry
                        {
                            Culture = cultureInfo.Culture,
                            Content = itemContent
                        };
                    }

                    if (itemContent.Value<bool>(nameof(GlobalPageSitemapSetting.SitemapHiddenTheirChildren), cultureInfo.Culture))
                    {
                        path = itemContent.Path;
                    }
                    else
                    {
                        path = string.Empty;
                    }
                }
            }
        }

        private sealed class SitemapEntry
        {
            public string Culture { get; init; } = string.Empty;
            public IPublishedContent? Content { get; init; }
        }
    }
}
