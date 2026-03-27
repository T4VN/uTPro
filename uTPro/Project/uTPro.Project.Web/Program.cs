using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Community.BlockPreview;
using Umbraco.Community.BlockPreview.Extensions;
using Umbraco.Community.BlockPreview.Interfaces;
using uTPro.Common.Constants;
using uTPro.Foundation.Middleware;
using uTPro.Project.Web.Configure;
using WebMarkupMin.AspNet.Common.Compressors;
using WebMarkupMin.AspNetCoreLatest;
using WebMarkupMin.Core;

var builder = WebApplication.CreateBuilder(args);

// Umbraco setup
var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddBlockPreview(options =>
    {
        options.BlockGrid.Enabled = true;
        options.BlockGrid.Stylesheets = [
            "/assets/css/blockgridlayout-backoffice.css",
            "/css/uTPro/layout.css",
            "/css/uTPro/main.css"
        ];
    });

umbracoBuilder.Build();

// Razor + runtime compilation
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// WebOptimizer (CSS/JS/HTML minify)
builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.MinifyCssFiles(new NUglify.Css.CssSettings()
    {
        IgnoreAllErrors = true,
        CommentMode = NUglify.Css.CssComment.None,
    }, "css/**/*.css", "assets/**/*.css");

    pipeline.MinifyJsFiles(
        new WebOptimizer.Processors.JsSettings(new NUglify.JavaScript.CodeSettings()
        {
            IgnoreAllErrors = true,
        }),
        "js/**/*.js", "assets/**/*.js"
    );

    pipeline.MinifyHtmlFiles();
});

// WebMarkupMin (compress + minify)
builder.Services.AddWebMarkupMin(options =>
{
    options.AllowMinificationInDevelopmentEnvironment = true;
    options.AllowCompressionInDevelopmentEnvironment = true;
    options.DisablePoweredByHttpHeaders = true;
    options.DisableMinification = false;
    options.DefaultEncoding = System.Text.Encoding.UTF8;
    options.MaxResponseSize = 10 * 1024 * 1024; // 10MB max for minification
}).AddHtmlMinification(options =>
{
    options.GenerateStatistics = true;
    options.MinificationSettings.AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.KeepQuotes;
}).AddXmlMinification().AddXhtmlMinification().AddHttpCompression(options =>
{
    options.CompressorFactories =
    [
        new GZipCompressorFactory(),
        new BuiltInBrotliCompressorFactory(),
        new DeflateCompressorFactory()
    ];
});

builder.Services.AddRenderingDefaults();

builder.Services.AddControllers();

// Configure Services
builder.Services.Configure<UmbracoRenderingDefaultsOptions>(c =>
{
    c.DefaultControllerType = typeof(ConfigureRenderController);
});

builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationExpanders.Add(new CustomBlockPreviewLocationExpander());
});
builder.Services.AddTransient<IBlockPreviewService, CustomBlockPreviewService>();
// Form + IIS + Kestrel config
builder.Services.Configure<FormOptions>(options =>
{
    options.BufferBody = true;
    options.ValueCountLimit = 10240;
    options.ValueLengthLimit = 4 * 1024 * 1024; // 4MB per value
    options.MultipartBoundaryLengthLimit = 128;
    options.MultipartHeadersCountLimit = 32;
    options.MultipartHeadersLengthLimit = 32768;
    options.MultipartBodyLengthLimit = 128L * 1024L * 1024L; // 128MB max upload
    options.BufferBodyLengthLimit = 128L * 1024L * 1024L;
}).Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
    options.MaxRequestBodySize = 128L * 1024L * 1024L; // 128MB
}).Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 128L * 1024L * 1024L; // 128MB
});

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(
        new DirectoryInfo(
            Path.Combine(
                builder.Environment.ContentRootPath,
                "umbraco",
                "Data",
                "TEMP",
                "PersistKeys")
            )
    )
    .SetApplicationName("uTPro")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

var app = builder.Build();
var env = app.Environment;

await app.BootUmbracoAsync();

// Middleware order
app.UseHttpsRedirection();
if (!env.IsDevelopment())
{
    app.UseHsts();
    app.Use(async (context, next) =>
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        await next();
    });
}

app.UseWebOptimizer();
app.UseWebMarkupMin();

app.UseCookiePolicy();
app.UseSession();
app.UseInitMiddleware();

// Umbraco pipeline
var appUmbraco = app.UseUmbraco();
var builderUm = appUmbraco.WithMiddleware(u =>
{
    u.UseBackOffice();
    u.UseWebsite();
});

builderUm.WithEndpoints(u =>
{
    u.EndpointRouteBuilder.MapControllers();
    u.UseBackOfficeEndpoints();
    u.UseWebsiteEndpoints();
});

app.MapControllers();

// Custom error handler + security headers
app.Use(async (context, next) =>
{
    // Security headers
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

    await next();

    // Only redirect 404 for website page requests (not backoffice domain, not static files, not API)
    if (context.Response.StatusCode == 404
        && !context.Response.HasStarted
        && !context.Request.Path.Value!.Contains('.'))
    {
        // Check if this is the backoffice domain — skip redirect for backoffice
        var boEnabled = builder.Configuration.GetValue<bool>(ConfigSettingUTPro.Backoffice.Enabled);
        var boDomain = builder.Configuration.GetValue<string>(ConfigSettingUTPro.Backoffice.Domain) ?? "";
        var host = context.Request.Host.Host;

        var isBackoffice = boEnabled
            && !string.IsNullOrEmpty(boDomain)
            && boDomain.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(d => host.Equals(d.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!isBackoffice)
        {
            context.Response.Redirect("/error");
        }
    }
});

await app.RunAsync();
