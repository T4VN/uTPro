using WebMarkupMin.AspNet.Common.Compressors;
using WebMarkupMin.AspNetCoreLatest;
using WebMarkupMin.Core;

namespace uTPro.Project.Web.Startup;

/// <summary>
/// Performance services: Razor, WebOptimizer (CSS/JS minify), WebMarkupMin (HTML minify + compression),
/// Output Caching, Response Caching. All tunable via appsettings (uTPro:Performance section).
/// </summary>
public static class PerformanceSetup
{
    public static IServiceCollection AddPerformanceServices(
        this IServiceCollection services,
        IWebHostEnvironment env,
        IConfiguration configuration)
    {
        var perfSection = configuration.GetSection("uTPro:Performance");

        // Razor + runtime compilation (dev only to save RAM/CPU in production)
        var mvcBuilder = services.AddRazorPages();
        if (env.IsDevelopment())
        {
            mvcBuilder.AddRazorRuntimeCompilation();
        }

        // ─── WebOptimizer: CSS/JS minification ───────────────────────────────────
        var enableDiskCache = perfSection.GetValue("WebOptimizer:EnableDiskCache", false);

        services.AddWebOptimizer(pipeline =>
        {
            pipeline.MinifyCssFiles(new NUglify.Css.CssSettings
            {
                IgnoreAllErrors = true,
                CommentMode = NUglify.Css.CssComment.None,
            }, "css/**/*.css", "assets/**/*.css", "uTPro/**/*.css");

            pipeline.MinifyJsFiles(
                new WebOptimizer.Processors.JsSettings(new NUglify.JavaScript.CodeSettings
                {
                    IgnoreAllErrors = true,
                }),
                "js/**/*.js", "assets/**/*.js", "scripts/**/*.js", "uTPro/**/*.js"
            );
        },
        options =>
        {
            options.EnableDiskCache = enableDiskCache;
            options.AllowEmptyBundle = true;
        });

        // ─── WebMarkupMin: HTML minification + compression ───────────────────────
        var maxResponseSizeMB = perfSection.GetValue("WebMarkupMin:MaxResponseSizeMB", 10);
        var isDev = env.IsDevelopment();

        services.AddWebMarkupMin(options =>
        {
            options.AllowMinificationInDevelopmentEnvironment = true;
            options.AllowCompressionInDevelopmentEnvironment = true;
            options.DisablePoweredByHttpHeaders = true;
            options.DisableMinification = false;
            options.DefaultEncoding = System.Text.Encoding.UTF8;
            options.MaxResponseSize = maxResponseSizeMB * 1024 * 1024;
        })
        .AddHtmlMinification(options =>
        {
            options.GenerateStatistics = isDev;
            options.MinificationSettings.AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.KeepQuotes;
        })
        .AddXmlMinification()
        .AddXhtmlMinification()
        .AddHttpCompression(options =>
        {
            options.CompressorFactories =
            [
                new BuiltInBrotliCompressorFactory(),
                new GZipCompressorFactory(),
                new DeflateCompressorFactory()
            ];
        });

        // ─── Output Caching ──────────────────────────────────────────────────────
        var outputCacheEnabled = perfSection.GetValue("OutputCache:Enabled", true);
        var sizeLimitMB = perfSection.GetValue("OutputCache:SizeLimitMB", 100);
        var defaultTTLSeconds = perfSection.GetValue("OutputCache:DefaultTTLSeconds", 120);
        var sitemapTTLMinutes = perfSection.GetValue("OutputCache:SitemapTTLMinutes", 30);

        services.AddOutputCache(options =>
        {
            options.SizeLimit = sizeLimitMB * 1024 * 1024;
            options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(defaultTTLSeconds);

            if (!outputCacheEnabled)
            {
                // When disabled, set a minimal expiration so entries expire almost immediately.
                options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(1);
                options.SizeLimit = 1;
                return;
            }

            options.AddPolicy("Page", policy =>
            {
                policy.Expire(TimeSpan.FromSeconds(defaultTTLSeconds));
                policy.SetVaryByHost(true);
                policy.SetVaryByQuery("page", "culture", "p");
                policy.SetVaryByHeader("Accept-Language");
                policy.Tag("pages");
            });

            options.AddPolicy("Sitemap", policy =>
            {
                policy.Expire(TimeSpan.FromMinutes(sitemapTTLMinutes));
                policy.SetVaryByHost(true);
                policy.Tag("sitemap");
            });
        });

        // Response Caching — Cache-Control validation for downstream proxies/CDN.
        services.AddResponseCaching();

        return services;
    }
}
