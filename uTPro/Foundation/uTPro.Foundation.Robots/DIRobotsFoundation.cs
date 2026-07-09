using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;

namespace uTPro.Foundation.Robots
{
    class DIRobotsFoundation : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
            => builder.Services.AddScoped<IRobotsFoundation, RobotsFoundation>();
    }
}
