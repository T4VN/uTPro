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

        // Data Protection keys. The ring decrypts auth cookies AND application data (e.g.
        // SimpleFormBuilder sensitive form entries), so losing it means permanently unreadable
        // payloads — it must live in a durable folder, never under a cleaned TEMP location.
        var dpKeysDir = ResolveDataProtectionKeysPath(builder);
        SeedRingFromLegacyLocations(dpKeysDir, builder.Environment.ContentRootPath);

        var dpBuilder = services.AddDataProtection()
            .PersistKeysToFileSystem(dpKeysDir)
            .SetApplicationName(builder.Configuration["uTPro:DataProtection:ApplicationName"] ?? "uTPro")
            .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

        // Encrypt the master key at rest (without this it is stored as plain XML on disk).
        // DPAPI is Windows-only; the machine scope lets every IIS app pool on this box decrypt.
        if (OperatingSystem.IsWindows())
            dpBuilder.ProtectKeysWithDpapi();

        return services;
    }

    private static DirectoryInfo ResolveDataProtectionKeysPath(WebApplicationBuilder builder)
    {
        var dpKeysPath = builder.Configuration["uTPro:DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(dpKeysPath))
        {
            // Durable default: next to the database and logs under umbraco/Data. Earlier builds
            // resolved this from LocalTempStorageLocation and could land in %TEMP% or
            // umbraco/Data/TEMP — both get cleaned, wiping the ring with every payload it
            // ever encrypted.
            dpKeysPath = Path.Combine(
                builder.Environment.ContentRootPath, "umbraco", "Data", "ProtectionKeys");
        }
        else if (!Path.IsPathRooted(dpKeysPath))
        {
            dpKeysPath = Path.GetFullPath(
                Path.Combine(builder.Environment.ContentRootPath, dpKeysPath));
        }

        var dir = new DirectoryInfo(dpKeysPath);
        if (!dir.Exists) dir.Create();
        return dir;
    }

    // One-time upgrade path: rings written by earlier builds sat in TEMP-style folders that
    // machine/OS cleanup can (and did) wipe. If the durable ring is brand new, pull in any key
    // files still present at the legacy spots so previously encrypted data stays readable.
    // Runs only when the target holds no keys yet — an existing ring is never touched.
    private static void SeedRingFromLegacyLocations(DirectoryInfo target, string contentRoot)
    {
        if (target.EnumerateFiles("key-*.xml").Any()) return;

        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(contentRoot)))
            .ToLowerInvariant();
        string[] legacyDirs =
        {
            Path.Combine(contentRoot, "umbraco", "Data", "TEMP", "PersistKeys"),
            Path.Combine(Path.GetTempPath(), "UmbracoData", hash, "PersistKeys"),
        };

        foreach (var legacy in legacyDirs)
        {
            if (!Directory.Exists(legacy)) continue;
            foreach (var keyFile in Directory.EnumerateFiles(legacy, "key-*.xml"))
                File.Copy(keyFile, Path.Combine(target.FullName, Path.GetFileName(keyFile)), overwrite: false);
        }
    }
}
