using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;

namespace uTPro.Project.Web.Configure
{
    class DIICheckPolicy : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
            => builder.Services.AddScoped<ICheckPolicy, CheckPolicy>();
    }
}
