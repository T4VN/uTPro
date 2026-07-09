using uTPro.Common.Constants;
using uTPro.Foundation.Middleware;
using WebMarkupMin.AspNetCoreLatest;

namespace uTPro.Project.Web.Startup;

/// <summary>
/// Middleware pipeline configuration: security headers, static files, caching,
/// compression, Umbraco routing.
/// </summary>
public static class PipelineSetup
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        var env = app.Environment;

        app.UseSecurityHeaders(env);
        app.UseHttpsRedirection();

        if (!env.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/error/{0}");
        app.UseWebOptimizer();
        app.UseStaticFilesWithCaching();
        app.UseWebMarkupMin();
        app.UseOutputCache();
        app.UseCookiePolicy();
        app.UseInitMiddleware();
        app.ConfigureUmbracoPipeline();
        app.MapControllers();

        return app;
    }

    private static void UseSecurityHeaders(this WebApplication app, IWebHostEnvironment env)
    {
        // The backoffice can live on a separate host (e.g. bo.utpro.local) from the
        // website (utpro.local or any other domain). Preview renders the website inside
        // an iframe hosted by the backoffice, so a blanket X-Frame-Options: SAMEORIGIN
        // blocks it — the two hosts are different origins and the preview cookie is not
        // shared across domains, so we cannot detect preview reliably on the frontend.
        //
        // Instead we always allow the trusted backoffice host to frame every page via CSP
        // frame-ancestors. This keeps clickjacking protection (only 'self' + the backoffice
        // may frame us) while letting cross-domain preview work regardless of frontend URL.
        // X-Frame-Options cannot express a specific cross-origin host (ALLOW-FROM is
        // deprecated), so when a backoffice host is configured we rely on frame-ancestors
        // alone; all evergreen browsers support it.
        var backofficeHosts = GetBackofficeHosts(app.Configuration);

        var frameAncestors = backofficeHosts.Length > 0
            ? "frame-ancestors 'self' " + string.Join(' ', backofficeHosts.Select(ToAncestorSource))
            : null;

        app.Use(async (context, next) =>
        {
            var header = context.Response.Headers;
            header.XContentTypeOptions = "nosniff";
            header["Referrer-Policy"] = "strict-origin-when-cross-origin";
            header["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            if (frameAncestors is not null)
            {
                // Separate backoffice host configured: use CSP frame-ancestors so the
                // backoffice can embed frontend pages for preview.
                header.ContentSecurityPolicy = frameAncestors;
            }
            else
            {
                // Single-domain setup: SAMEORIGIN is enough for preview.
                header.XFrameOptions = "SAMEORIGIN";
            }

            if (!env.IsDevelopment())
            {
                header.StrictTransportSecurity = "max-age=31536000; includeSubDomains";
            }

            // Browser cache for HTML pages (not backoffice, not static files)
            var path = context.Request.Path.Value ?? string.Empty;
            var isWebsitePage = context.Request.Method == "GET"
                && !path.StartsWith("/umbraco", StringComparison.OrdinalIgnoreCase)
                && !path.StartsWith("/app_plugins", StringComparison.OrdinalIgnoreCase)
                && !path.Contains('.');

            if (isWebsitePage)
            {
                context.Response.OnStarting(() =>
                {
                    if (context.Response.StatusCode == 200
                        && !context.Response.Headers.ContainsKey("Cache-Control"))
                    {
                        context.Response.Headers.CacheControl =
                            "public, max-age=120, stale-while-revalidate=60";
                    }
                    return Task.CompletedTask;
                });
            }

            await next();
        });
    }

    // Reads the configured backoffice host(s). appsettings.Development.json uses the
    // "Url" key (matches the ConfigSettingUTPro constant) while appsettings.json uses
    // "Domain", so we check both. Supports a comma/semicolon separated list.
    private static string[] GetBackofficeHosts(IConfiguration config)
    {
        var value = config.GetSection(ConfigSettingUTPro.Backoffice.Domain)?.Value
            ?? config.GetSection(ConfigSettingUTPro.Backoffice.Key + ":Domain")?.Value;

        return value?.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [];
    }

    // Normalise a configured backoffice host into a CSP frame-ancestors source.
    // Bare hosts (bo.utpro.local) get an https:// scheme; explicit schemes are kept as-is.
    private static string ToAncestorSource(string host)
    {
        host = host.Trim();
        return host.Contains("://", StringComparison.Ordinal) ? host : "https://" + host;
    }

    private static void UseStaticFilesWithCaching(this WebApplication app)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                var path = ctx.Context.Request.Path.Value ?? string.Empty;
                if (path.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/scripts/", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase))
                {
                    // Versioned/fingerprinted assets: 1 year, immutable
                    ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
                }
                else if (path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWith("/media/", StringComparison.OrdinalIgnoreCase))
                {
                    // CMS media uploads: 7 days cache. Files have unique GUID-based
                    // names so re-uploads get new URLs automatically.
                    ctx.Context.Response.Headers.CacheControl = "public,max-age=604800,stale-while-revalidate=86400";
                }
            }
        });
    }

    private static void ConfigureUmbracoPipeline(this WebApplication app)
    {
        app.UseUmbraco()
            .WithMiddleware(u =>
            {
                u.UseBackOffice();
                u.UseWebsite();
            })
            .WithEndpoints(u =>
            {
                u.EndpointRouteBuilder.MapControllers();
                u.UseBackOfficeEndpoints();
                u.UseWebsiteEndpoints();
            });
    }
}
