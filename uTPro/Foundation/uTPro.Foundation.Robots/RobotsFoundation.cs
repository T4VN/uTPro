using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Web.Common.PublishedModels;
using uTPro.Extension.CurrentSite;

namespace uTPro.Foundation.Robots
{
    class DIRobotsFoundation : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
            => builder.Services.AddScoped<IRobotsFoundation, RobotsFoundation>();
    }

    public interface IRobotsFoundation
    {
        string Generate();
    }

    internal class RobotsFoundation : IRobotsFoundation
    {
        readonly IHttpContextAccessor _httpContextAccessor;
        readonly ICurrentSiteExtension _currentSite;
        const string _contentTempalte = @"User-agent: *
Sitemap: {{Url}}sitemap.xml
";

        public RobotsFoundation(ICurrentSiteExtension currentSite, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _currentSite = currentSite;
        }

        public string Generate()
        {
            string template = string.Empty;
            var rootNode = _currentSite.GetItem().Root;
            if (rootNode != null)
            {
                template = rootNode?.Descendant()?.GetProperty(nameof(GlobalSitemapRobotsSettings.RobotstxtContent))?.GetValue()?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(template))
                {
                    template = _contentTempalte.Replace("{{Url}}", GetAbsoluteUri().ToString());
                }
            }

            return template;
        }

        private Uri GetAbsoluteUri()
        {
            var request = _httpContextAccessor?.HttpContext?.Request;
            UriBuilder uriBuilder = new UriBuilder();
            uriBuilder.Scheme = request?.Scheme;
            uriBuilder.Host = request?.Host.Host;
            return uriBuilder.Uri;
        }

    }
}
