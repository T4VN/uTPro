using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Xml.Linq;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Web.Common.PublishedModels;
using uTPro.Common.Constants;
using uTPro.Extension;
using uTPro.Extension.CurrentSite;

namespace uTPro.Foundation.Sitemap
{
    class DISitemapFoundation : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
            => builder.Services.AddScoped<ISitemapFoundation, SitemapFoundation>();
    }

    public interface ISitemapFoundation
    {
        string Generate();
    }

    internal class SitemapFoundation : ISitemapFoundation
    {
        const string fileSitemapXSL = "sitemap.xsl";

        static readonly XNamespace schemas_Sitemap = "http://www.sitemaps.org/schemas/sitemap/0.9";
        static readonly XNamespace schemas_xhtml = "http://www.w3.org/1999/xhtml";
        static readonly XNamespace schemas_xsd = "http://www.w3.org/2001/XMLSchema";
        static readonly XNamespace schemas_xsi = "http://www.w3.org/2001/XMLSchema-instance";
        private class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding { get { return Encoding.UTF8; } }
        }

        readonly ICurrentSiteExtension _currentSite;
        public SitemapFoundation(ICurrentSiteExtension currentSite)
        {
            _currentSite = currentSite;
        }

        public string Generate()
        {
            XElement root = InitElementRoot();
            var nodes = GetListNodes();
            if (nodes != null)
            {
                foreach (PublishedContentCulture dataNode in nodes)
                {
                    string culture = dataNode.Culture;// item.Value.Culture;
                    var node = dataNode.ListContent;// item.Value.Culture;
                    if (node != null)
                    {
                        XElement urlElement = new XElement(schemas_Sitemap + "url");
                        ValueLoc(node, culture, urlElement);
                        ValueLastMod(node, urlElement);
                        ValueChangeFreqency(node, culture, urlElement);
                        ValuePriority(node, culture, urlElement);
                        ValueXHTMLLink(node, culture, urlElement);

                        root.Add(urlElement);
                    }
                }
            }
            XDocument document = new XDocument(new XDeclaration("1.0", "UTF-8", null), root);
            return ConvertDocumentToString(document);
        }

        private void ValueLoc(IPublishedContent node, string culture, XElement element)
        {
            element.Add(new XElement(schemas_Sitemap + "loc", node.Url(culture, mode: UrlMode.Absolute)));
        }

        private void ValueLastMod(IPublishedContent node, XElement element)
        {
            element.Add(new XElement(schemas_Sitemap + "lastmod", node.UpdateDate.ToString("yyyy-MM-dd")));
        }

        private void ValueChangeFreqency(IPublishedContent node, string culture, XElement element)
        {
            string changeFreqency = string.Empty;
            try
            {
                changeFreqency = node.ValueToString(nameof(GlobalPageSitemapSetting.SitemapXmlChangeFrequency), culture);
            }
            catch (Exception) { }
            if (string.IsNullOrWhiteSpace(changeFreqency) == false)
            {
                element.Add(new XElement(schemas_Sitemap + "changefreq", changeFreqency.ToLower()));
            }
        }

        private void ValuePriority(IPublishedContent node, string culture, XElement element)
        {
            decimal priority = -1;
            try
            {
                priority = node.Value<decimal>(nameof(GlobalPageSitemapSetting.SitemapXmlPriority), culture);
            }
            catch (Exception) { }
            if (priority >= 0)
            {
                element.Add(new XElement(schemas_Sitemap + "priority", priority));
            }
            else
            {
                priority = node.Level - 1;
                priority = 1.1M - (priority / 10);
                element.Add(new XElement(schemas_Sitemap + "priority", priority));
            }
        }

        private void ValueXHTMLLink(IPublishedContent node, string culture, XElement element)
        {
            foreach (var itemCul in node.Cultures)
            {
                if (GetHiddenSitemap(node, itemCul.Value.Culture))
                {
                    continue;
                }
                var elementCul = new XElement(schemas_xhtml + "link",
                    new XAttribute("rel", "alternate"),
                    new XAttribute("hreflang", itemCul.Value.Culture),
                    new XAttribute("href", node.Url(itemCul.Value.Culture, mode: UrlMode.Absolute)));
                element.Add(elementCul);
            }
        }

        private XElement InitElementRoot()
        {
            return new XElement(schemas_Sitemap + "urlset"
                , new XAttribute(XNamespace.Xmlns + "xhtml", schemas_xhtml)
                , new XAttribute(XNamespace.Xmlns + "xsd", schemas_xsd)
                , new XAttribute(XNamespace.Xmlns + "xsi", schemas_xsi)
                );
        }

        private bool GetHiddenSitemap(IPublishedContent node, string culture)
        {
            return node.Value<bool>(nameof(GlobalPageSitemapSetting.SitemapHiddenSitemap), culture);
        }

        private bool GetHiddenTheirChildrenSitemap(IPublishedContent node, string culture)
        {
            return node.Value<bool>(nameof(GlobalPageSitemapSetting.SitemapHiddenTheirChildren), culture);
        }

        private string ConvertDocumentToString(XDocument document)
        {
            StringWriter sw = new Utf8StringWriter();
            sw.WriteLine(document.Declaration?.ToString());
            if (System.IO.File.Exists(Path.Combine(PathFolder.DirectoryWWWRoot, fileSitemapXSL)))
            {
                sw.WriteLine("<?xml-stylesheet type=\"text/xsl\" href=\"/" + fileSitemapXSL + "\"?>");
            }
            sw.WriteLine(document.ToString());
            return sw.ToString();
        }

        private IEnumerable<PublishedContentCulture> GetListNodes()
        {
            IList<PublishedContentCulture> lstCulture = new List<PublishedContentCulture>();
            var rootNode = _currentSite.GetItem().PageHome;
            if (rootNode == null)
            {
                return lstCulture;
            }
            var culs = _currentSite.GetCultures();
            List<string> lstItemHiddenParent = new List<string>();
            foreach (var item in culs)
            {
                string path = string.Empty;
                var lstContent = rootNode.DescendantsOrSelf(item.Culture).OrderBy(x => x.Path).ToList();
                foreach (var itemContent in lstContent)
                {
                    if (!string.IsNullOrEmpty(path) && itemContent.Path.IndexOf(path) == 0)
                    {
                        continue;
                    }

                    if (!GetHiddenSitemap(itemContent, item.Culture))
                    {
                        lstCulture.Add(new PublishedContentCulture()
                        {
                            Culture = item.Culture,
                            ListContent = itemContent
                        });
                    }
                    if (!GetHiddenTheirChildrenSitemap(itemContent, item.Culture))
                    {
                        path = string.Empty;
                    }
                    else
                    {
                        path = itemContent.Path;
                    }
                }
            }
            return lstCulture;
        }

        private class PublishedContentCulture
        {
            public string Culture { get; set; } = string.Empty;
            public IPublishedContent? ListContent { get; set; }
        }
    }
}
