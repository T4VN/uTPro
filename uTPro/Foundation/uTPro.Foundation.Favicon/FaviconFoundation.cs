using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Web.Common.PublishedModels;
using uTPro.Extension;
using uTPro.Extension.CurrentSite;

namespace uTPro.Foundation.Favicon
{
    class DIFaviconFoundation : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
            => builder.Services.AddScoped<IFaviconFoundation, FaviconFoundation>();
    }

    public interface IFaviconFoundation
    {
        string Generate();
    }

    internal class FaviconFoundation : IFaviconFoundation
    {
        readonly ICurrentSiteExtension _currentSite;
        readonly string imageDefault = "/favicon-default.ico";

        public FaviconFoundation(ICurrentSiteExtension currentSite)
        {
            _currentSite = currentSite;
        }

        public string Generate()
        {
            try
            {
                var favicon = _currentSite.GetItem().FolderSettings;
                if (favicon != null)
                {
                    string img = favicon.ValueToMediaMobile(nameof(GlobalSiteSettings.SiteSettingsfaviconIco));
                    if (!string.IsNullOrWhiteSpace(img))
                    {
                        return img;
                    }
                }
            }
            catch (Exception)
            {
            }
            
            return imageDefault;
        }
    }
}
