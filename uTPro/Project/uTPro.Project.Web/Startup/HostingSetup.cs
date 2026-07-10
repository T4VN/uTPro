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
    /// Override the web root (wwwroot) from appsettings (uTPro:Hosting:RootPath).
    /// Lets multiple apps (e.g. a backoffice app and a front-end app) point at the same
    /// physical wwwroot so uploaded media / static files are shared between them.
    /// A relative value is resolved against ContentRootPath. Empty = default ./wwwroot.
    /// Must run before ConfigureUmbraco() so Umbraco picks up the correct web root.
    /// </summary>
    public static WebApplicationBuilder ConfigureRootPath(this WebApplicationBuilder builder)
    {
        var customRootPath = builder.Configuration["uTPro:Hosting:RootPath"];
        if (string.IsNullOrWhiteSpace(customRootPath))
            return builder;

        if (!Path.IsPathRooted(customRootPath))
        {
            customRootPath = Path.GetFullPath(
                Path.Combine(builder.Environment.ContentRootPath, customRootPath));
        }

        Directory.CreateDirectory(customRootPath);

        // Deferred by WebApplicationBuilder until Build(), so it correctly rebuilds
        // the WebRootFileProvider used by static files and Umbraco.
        builder.WebHost.UseWebRoot(customRootPath);
        builder.Environment.WebRootPath = customRootPath;

        return builder;
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
