using Umbraco.Community.BlockPreview.Extensions;

namespace uTPro.Project.Web.Startup;

/// <summary>
/// Umbraco CMS builder configuration.
/// </summary>
public static class UmbracoSetup
{
    public static WebApplicationBuilder ConfigureUmbraco(this WebApplicationBuilder builder)
    {
        builder.CreateUmbracoBuilder()
            .AddBackOffice()
            .AddWebsite()
            .AddComposers()
            .AddBlockPreview(options =>
            {
                options.BlockGrid.Enabled = true;
                options.BlockGrid.Stylesheets =
                [
                    "/assets/css/blockgridlayout-backoffice.css",
                    "/css/uTPro/layout.css",
                    "/css/uTPro/main.css"
                ];
            })
            .Build();

        return builder;
    }
}
