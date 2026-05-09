namespace uTPro.Project.Web.Startup;

/// <summary>
/// Hosting environment setup: TMP/TEMP override for multi-site IIS deployments.
/// </summary>
public static class HostingSetup
{
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
}
