using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace uTPro.Extension.CurrentSite
{
    public interface ICurrentSiteExtension
    {
        IConfiguration Configuration { get; }
        IWebHostEnvironment WebHostEnvironment { get; }
        string DefaultCulture { get; }
        CultureInfo CurrentCulture { get; }
        string CurrentPage { get; }
        IEnumerable<PublishedCultureInfo> GetCultures();
        IUmbracoContext UContext { get; }
        string GetDictionaryValue(string key, string valueDefault = "", bool showKey = false);
        void SetCurrentCulture(CultureInfo cul);
        ICurrentItemExtension GetItem();
        IReadOnlyList<Domain> GetDomains(bool isGetAll);
        string GetUrlWithCulture(IPublishedContent content, string? culture = null, UrlMode mode = UrlMode.Default);
    }
}
