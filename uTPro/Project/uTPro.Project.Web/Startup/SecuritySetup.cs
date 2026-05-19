using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;

namespace uTPro.Project.Web.Startup;

/// <summary>
/// Security services: Data Protection, Form/IIS/Kestrel limits, server hardening.
/// </summary>
public static class SecuritySetup
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services, WebApplicationBuilder builder)
    {
        // Form + IIS + Kestrel config
        services.Configure<FormOptions>(options =>
        {
            options.BufferBody = false;
            options.ValueCountLimit = 10240;
            options.ValueLengthLimit = 4 * 1024 * 1024;
            options.MultipartBoundaryLengthLimit = 128;
            options.MultipartHeadersCountLimit = 32;
            options.MultipartHeadersLengthLimit = 32768;
            options.MultipartBodyLengthLimit = 128L * 1024L * 1024L;
        })
        .Configure<IISServerOptions>(options =>
        {
            options.AllowSynchronousIO = false;
            options.MaxRequestBodySize = 128L * 1024L * 1024L;
        })
        .Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
        {
            options.AddServerHeader = false;
            options.AllowSynchronousIO = false;
            options.Limits.MaxRequestBodySize = 128L * 1024L * 1024L;
        });

        // Data Protection keys
        var dpKeysDir = ResolveDataProtectionKeysPath(builder);
        services.AddDataProtection()
            .PersistKeysToFileSystem(dpKeysDir)
            .SetApplicationName(builder.Configuration["uTPro:DataProtection:ApplicationName"] ?? "uTPro")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

        return services;
    }

    private static DirectoryInfo ResolveDataProtectionKeysPath(WebApplicationBuilder builder)
    {
        var dpKeysPath = builder.Configuration["uTPro:DataProtection:KeysPath"];

        if (!string.IsNullOrWhiteSpace(dpKeysPath))
        {
            if (!Path.IsPathRooted(dpKeysPath))
            {
                dpKeysPath = Path.GetFullPath(
                    Path.Combine(builder.Environment.ContentRootPath, dpKeysPath));
            }
        }
        else
        {
            var localTempStorage = builder.Configuration["Umbraco:CMS:Hosting:LocalTempStorageLocation"];
            if (string.Equals(localTempStorage, "EnvironmentTemp", StringComparison.OrdinalIgnoreCase))
            {
                var hashBytes = System.Security.Cryptography.SHA1.HashData(
                    System.Text.Encoding.UTF8.GetBytes(builder.Environment.ContentRootPath));
                var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
                dpKeysPath = Path.Combine(Path.GetTempPath(), "UmbracoData", hash, "PersistKeys");
            }
            else
            {
                dpKeysPath = Path.Combine(
                    builder.Environment.ContentRootPath,
                    "umbraco", "Data", "TEMP", "PersistKeys");
            }
        }

        var dir = new DirectoryInfo(dpKeysPath);
        if (!dir.Exists) dir.Create();
        return dir;
    }
}
