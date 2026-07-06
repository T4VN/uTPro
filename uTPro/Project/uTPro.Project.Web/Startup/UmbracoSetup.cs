using Our.Umbraco.PostgreSql;
using Umbraco.Community.BlockPreview.Extensions;

namespace uTPro.Project.Web.Startup;

/// <summary>
/// Umbraco CMS builder configuration.
/// </summary>
public static class UmbracoSetup
{
    public static WebApplicationBuilder ConfigureUmbraco(this WebApplicationBuilder builder)
    {
        // Only wire up the PostgreSQL provider when the configured provider is PostgreSQL.
        // Our.Umbraco.PostgreSql registers its factory as "Npgsql2", so a case-insensitive
        // "Npgsql" contains-check matches both "Npgsql" and "Npgsql2". SQL Server / SQLite skip it.
        var providerName = builder.Configuration.GetConnectionString("umbracoDbDSN_ProviderName");
        var usePostgreSql = providerName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

        var umbracoBuilder = builder.CreateUmbracoBuilder()
            .AddBackOffice()
            .AddWebsite()
            .AddComposers();

        if (usePostgreSql)
        {
            umbracoBuilder.AddUmbracoPostgreSqlSupport();
        }

        umbracoBuilder
            .AddBlockPreview(options =>
            {
                options.BlockGrid.Enabled = true;
                options.BlockGrid.Stylesheets =
                [
                    "/assets/css/blockgridlayout-backoffice.css",
                    "/css/uTPro/variables.css",
                    "/css/uTPro/layout.css",
                    "/css/uTPro/main.css",
                    "/css/uTPro/icon.css"
                ];
            })
            .Build();

        return builder;
    }
}
