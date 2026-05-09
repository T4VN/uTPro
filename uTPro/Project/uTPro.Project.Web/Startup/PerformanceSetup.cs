using WebMarkupMin.AspNet.Common.Compressors;
using WebMarkupMin.AspNetCoreLatest;
using WebMarkupMin.Core;

namespace uTPro.Project.Web.Startup;

/// <summary>
/// Performance services: Razor, WebOptimizer (CSS/JS minify), WebMarkupMin (HTML minify + compression),
/// Output Caching, Response Caching.
/// </summary>
public static class PerformanceSetup
{
    public static IServiceCollection AddPerformanceServices(this IServiceCollection services, IWebHostEnvironment env)
    {
        // Razor + runtime compilation (dev only to save RAM/CPU in production)
        var mvcBuilder = services.AddRazorPages();
        if (env.IsDevelopment())
        {
            mvcBuilder.AddRazorRuntimeCompilation();
        }

        // WebOptimizer: CSS/JS minification in ALL environments.
        // By default WebOptimizer skips processing in Development — we override
        // via the options callback to force minification always on.
        services.AddWebOptimizer(pipeline =>
        {
            pipeline.MinifyCssFiles(new NUglify.Css.CssSettings
            {
                IgnoreAllErrors = true,
                CommentMode = NUglify.Css.CssComment.None,
            }, "css/**/*.css", "assets/**/*.css");

            pipeline.MinifyJsFiles(
                new WebOptimizer.Processors.JsSettings(new NUglify.JavaScript.CodeSettings
                {
                    IgnoreAllErrors = true,
                }),
                "js/**/*.js", "assets/**/*.js", "scripts/**/*.js"
            );
        },
        // Force minification even in Development
        options =>
        {
            options.EnableDiskCache = false;
            options.AllowEmptyBundle = true;
        });

        // WebMarkupMin: HTML minification + Brotli/GZip/Deflate compression
        var isDev = env.IsDevelopment();
        services.AddWebMarkupMin(options =>
        {
            options.AllowMinificationInDevelopmentEnvironment = true;
            options.AllowCompressionInDevelopmentEnvironment = true;
            options.DisablePoweredByHttpHeaders = true;
            options.DisableMinification = false;
            options.DefaultEncoding = System.Text.Encoding.UTF8;
            options.MaxResponseSize = 10 * 1024 * 1024;
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

        // Output Caching — stores fully-rendered responses in memory.
        services.AddOutputCache(options =>
        {
            options.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(120);

            options.AddPolicy("Page", policy =>
            {
                policy.Expire(TimeSpan.FromSeconds(120));
                policy.SetVaryByHost(true);
                policy.SetVaryByQuery("page", "culture", "p");
                policy.SetVaryByHeader("Accept-Language");
                policy.Tag("pages");
            });

            options.AddPolicy("Sitemap", policy =>
            {
                policy.Expire(TimeSpan.FromMinutes(30));
                policy.SetVaryByHost(true);
                policy.Tag("sitemap");
            });
        });

        // Response Caching — Cache-Control validation for downstream proxies/CDN.
        services.AddResponseCaching();

        return services;
    }
}
