using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Common.Routing;
using Umbraco.Cms.Web.Website.Controllers;

namespace uTPro.Project.Web.Configure
{
    public static partial class InitConfigure
    {
        public static void AddRenderingDefaults(this IServiceCollection services)
        {
            // Configure Umbraco Render Controller Type
            services?.Configure<UmbracoRenderingDefaultsOptions>(c =>
            {
                c.DefaultControllerType = typeof(ConfigureRenderController);
            });
        }
    }
}
