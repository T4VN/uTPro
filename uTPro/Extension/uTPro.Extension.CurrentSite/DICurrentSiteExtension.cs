using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;

namespace uTPro.Extension.CurrentSite
{
    class DICurrentSiteExtension : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
            => builder.Services.AddScoped<ICurrentSiteExtension, CurrentSiteExtension>();
    }
}
