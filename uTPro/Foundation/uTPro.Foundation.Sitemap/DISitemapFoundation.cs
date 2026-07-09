using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;

namespace uTPro.Foundation.Sitemap
{
    class DISitemapFoundation : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
            => builder.Services.AddScoped<ISitemapFoundation, SitemapFoundation>();
    }
}
