using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Razor;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Community.BlockPreview.Extensions;
using uTPro.Configure;
using uTPro.Foundation.Middleware;
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
        options.BlockGrid.Stylesheet = "/assets/css/blockgridlayout-backoffice.css";
    });

umbracoBuilder.Build();

// Razor + runtime compilation
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
builder.Services.AddSession();

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
    options.MaxResponseSize = int.MaxValue;
}).AddHtmlMinification(options =>
{
    options.GenerateStatistics = true;
    options.MinificationSettings.AttributeQuotesRemovalMode = HtmlAttributeQuotesRemovalMode.KeepQuotes;
}).AddXmlMinification().AddXhtmlMinification().AddHttpCompression(options =>
{
    options.CompressorFactories = new List<ICompressorFactory>
    {
        new GZipCompressorFactory(),
        new BuiltInBrotliCompressorFactory(),
        new DeflateCompressorFactory()
    };
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

builder.Services.Configure<UmbracoRequestPathsOptions>(options =>
{
    options.IsBackOfficeRequest = path =>
    {
        return path.StartsWith("/umbraco", StringComparison.OrdinalIgnoreCase);
    };
});

// Form + IIS + Kestrel config
builder.Services.Configure<FormOptions>(options =>
{
    options.BufferBody = true;
    options.ValueCountLimit = int.MaxValue;
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBoundaryLengthLimit = int.MaxValue;
    options.MultipartHeadersCountLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue;
    options.BufferBodyLengthLimit = 4L * 1024L * 1024L * 1024L;
}).Configure<IISServerOptions>(options =>
{
    options.AllowSynchronousIO = true;
    options.MaxRequestBodySize = long.MaxValue;
}).Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = long.MaxValue;
});

var app = builder.Build();
var env = app.Environment;

await app.BootUmbracoAsync();

// Middleware order
app.UseHttpsRedirection();
if (!env.IsDevelopment())
{
    app.UseHsts();
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

// Custom error handler
app.Use(async (context, next) =>
{
    //context.Response.Headers.Add("X-Xss-Protection", "1; mode=block");
    //context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000");
    //context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    //context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
    await next();
    if (context.Response.StatusCode == 404)
    {
        context.Response.Redirect("/error");
    }
});

await app.RunAsync();
