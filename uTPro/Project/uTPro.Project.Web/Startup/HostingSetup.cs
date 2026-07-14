namespace uTPro.Project.Web.Startup;

/// <summary>
/// Hosting environment setup: TMP/TEMP and wwwroot overrides for multi-site / multi-app deployments.
/// </summary>
public static class HostingSetup
{
    public static WebApplicationBuilder ConfigureAppSettings(this WebApplicationBuilder builder)
    {
        var appVariant = Environment.GetEnvironmentVariable("UTPRO_APP") ?? builder.Configuration["uTPro:App"];
        if (!string.IsNullOrWhiteSpace(appVariant))
        {
            builder.Configuration
                .AddJsonFile($"appsettings.{appVariant}.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{appVariant}.{builder.Environment.EnvironmentName}.json",
                    optional: true, reloadOnChange: true);
        }

        return builder;
    }
    /// <summary>
    /// Override TMP/TEMP per site from appsettings (uTPro:Hosting:TempPath).
    /// Must run before anything that calls Path.GetTempPath() (e.g. Umbraco with
    /// LocalTempStorageLocation=EnvironmentTemp, Examine TempFileSystemDirectoryFactory).
    /// </summary>
    public static WebApplicationBuilder ConfigureTempPath(this WebApplicationBuilder builder)
    {
        var customTempPath = builder.Configuration["uTPro:Hosting:TempPath"];
        if (string.IsNullOrWhiteSpace(customTempPath))
            return builder;

        if (!Path.IsPathRooted(customTempPath))
        {
            customTempPath = Path.GetFullPath(
                Path.Combine(builder.Environment.ContentRootPath, customTempPath));
        }

        Directory.CreateDirectory(customTempPath);
        Environment.SetEnvironmentVariable("TMP", customTempPath);
        Environment.SetEnvironmentVariable("TEMP", customTempPath);

        return builder;
    }

    /// <summary>
    /// Build the <see cref="WebApplicationOptions"/> for <c>WebApplication.CreateBuilder</c>,
    /// applying the web root (wwwroot) override from appsettings (uTPro:Hosting:RootPath).
    /// The web root MUST be set here: <c>WebApplicationBuilder</c> does not allow changing it
    /// afterwards (builder.WebHost.UseWebRoot throws NotSupportedException). A small bootstrap
    /// configuration is read first (appsettings.json + appsettings.{env}.json + the UTPRO_APP
    /// overlay + environment variables) so the value can come from any of those, including the
    /// per-app overlay. A relative value is resolved against the content root. Empty = default ./wwwroot.
    /// </summary>
    public static WebApplicationOptions BuildWebApplicationOptions(string[] args)
    {
        var contentRoot = Directory.GetCurrentDirectory();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";

        var bootstrap = new ConfigurationBuilder()
            .SetBasePath(contentRoot)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true);

        var appVariant = Environment.GetEnvironmentVariable("UTPRO_APP")
            ?? bootstrap.Build()["uTPro:App"];
        if (!string.IsNullOrWhiteSpace(appVariant))
        {
            bootstrap
                .AddJsonFile($"appsettings.{appVariant}.json", optional: true)
                .AddJsonFile($"appsettings.{appVariant}.{environment}.json", optional: true);
        }

        var customRootPath = bootstrap.AddEnvironmentVariables().Build()["uTPro:Hosting:RootPath"];
        if (string.IsNullOrWhiteSpace(customRootPath))
            return new WebApplicationOptions { Args = args };

        if (!Path.IsPathRooted(customRootPath))
            customRootPath = Path.GetFullPath(Path.Join(contentRoot, customRootPath));

        Directory.CreateDirectory(customRootPath);

        return new WebApplicationOptions { Args = args, WebRootPath = customRootPath };
    }

    /// <summary>
    /// Point Umbraco's physical media folder at a shared location from appsettings
    /// (uTPro:Hosting:MediaPath) by feeding Umbraco:CMS:Global:UmbracoMediaPhysicalRootPath.
    /// Prefer this over ConfigureRootPath when only uploaded media (logos, images, files)
    /// needs to be shared between apps — App_Plugins and compiled assets stay per-app.
    /// A relative value is resolved against ContentRootPath. Empty = default (~/media in wwwroot).
    /// Must run before ConfigureUmbraco().
    /// </summary>
    public static WebApplicationBuilder ConfigureMediaPath(this WebApplicationBuilder builder)
    {
        var customMediaPath = builder.Configuration["uTPro:Hosting:MediaPath"];
        if (string.IsNullOrWhiteSpace(customMediaPath))
            return builder;

        if (!Path.IsPathRooted(customMediaPath))
        {
            customMediaPath = Path.GetFullPath(
                Path.Combine(builder.Environment.ContentRootPath, customMediaPath));
        }

        Directory.CreateDirectory(customMediaPath);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Umbraco:CMS:Global:UmbracoMediaPhysicalRootPath"] = customMediaPath
        });

        return builder;
    }
}
