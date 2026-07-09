using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;

namespace uTPro.Foundation.Favicon
{
    class DIFaviconFoundation : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
            => builder.Services.AddScoped<IFaviconFoundation, FaviconFoundation>();
    }
}
