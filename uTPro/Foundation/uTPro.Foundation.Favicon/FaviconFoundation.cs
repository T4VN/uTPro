using Umbraco.Cms.Web.Common.PublishedModels;
using uTPro.Extension;
using uTPro.Extension.CurrentSite;

namespace uTPro.Foundation.Favicon
{
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
