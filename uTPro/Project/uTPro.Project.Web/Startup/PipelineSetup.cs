using uTPro.Foundation.Middleware;
using WebMarkupMin.AspNetCoreLatest;

namespace uTPro.Project.Web.Startup;

/// <summary>
/// Middleware pipeline configuration: frontend caching, static files, compression,
/// Umbraco routing. Security headers (incl. HSTS and cross-domain backoffice preview)
/// are owned by <see cref="SecurityHeadersMiddleware"/>, self-registered via its composer
/// and driven per-site by the GlobalSecurityHeadersSettings node (appsettings fallback).
/// </summary>
public static class PipelineSetup
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseFrontendCaching();
        app.UseHttpsRedirection();

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

    // Browser cache hint for rendered HTML pages (not backoffice, not static files).
    // Security headers themselves are handled by SecurityHeadersMiddleware.
    private static void UseFrontendCaching(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
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
