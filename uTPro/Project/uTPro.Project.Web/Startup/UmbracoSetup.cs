using Our.Umbraco.PostgreSql;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Sync;
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

        umbracoBuilder.ConfigureServerRole(builder);

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

    /// <summary>
    /// Pin the Umbraco <see cref="ServerRole"/> from appsettings (uTPro:Hosting:ServerRole)
    /// for explicit load-balancing. Accepted values: "SchedulingPublisher", "Subscriber",
    /// "Single". Empty or "Auto" leaves Umbraco's default database election in place.
    /// When the app is a Subscriber, uSync first-boot import is disabled so it doesn't
    /// race/duplicate the import that the SchedulingPublisher already runs against the shared DB.
    /// </summary>
    private static void ConfigureServerRole(this IUmbracoBuilder umbracoBuilder, WebApplicationBuilder builder)
    {
        var configured = builder.Configuration["uTPro:Hosting:ServerRole"];
        if (string.IsNullOrWhiteSpace(configured) ||
            configured.Equals("Auto", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!Enum.TryParse<ServerRole>(configured, ignoreCase: true, out var role) ||
            role == ServerRole.Unknown)
        {
            throw new InvalidOperationException(
                $"Invalid uTPro:Hosting:ServerRole value '{configured}'. " +
                "Expected: SchedulingPublisher, Subscriber, Single, or Auto.");
        }

        umbracoBuilder.Services.AddUnique<IServerRoleAccessor>(new ConfigurableServerRoleAccessor(role));

        // On a shared database only the SchedulingPublisher owns schema changes and the
        // uSync import. Subscribers must skip both, otherwise concurrent boots race on
        // migrations / duplicate the uSync import.
        if (role == ServerRole.Subscriber)
        {
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["uSync:Settings:ImportOnFirstBoot"] = "false",
                ["Umbraco:CMS:Unattended:UpgradeUnattended"] = "false"
            });
        }
    }
}
