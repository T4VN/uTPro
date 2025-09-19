using Microsoft.AspNetCore.Http.Features;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Website.Controllers;
using Umbraco.Community.BlockPreview.Extensions;
using uTPro.Configure;
using uTPro.Foundation.Middleware;
using WebMarkupMin.AspNet.Common.Compressors;
using WebMarkupMin.AspNetCoreLatest;
using WebMarkupMin.Core;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddBlockPreview(options =>
    {
        options.BlockGrid = new()
        {
            Enabled = true,
        };
        options.BlockList = new()
        {
            Enabled = true,
        };
        options.RichText.Enabled = true;
    })
    .Build();

builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
//builder.Services.AddResponseCompression();

builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.MinifyCssFiles(new NUglify.Css.CssSettings()
    {
        IgnoreAllErrors = true,
        CommentMode = NUglify.Css.CssComment.None,
    }, "css/**/*.css", "lib/**/*.css");

    pipeline.MinifyJsFiles(
        new WebOptimizer.Processors.JsSettings(new NUglify.JavaScript.CodeSettings()
        {
            IgnoreAllErrors = true,
        })
    , "js/**/*.js", "lib/**/*.js");

    pipeline.MinifyHtmlFiles();
});

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

builder.Services.Configure<UmbracoRenderingDefaultsOptions>(c =>
{
    c.DefaultControllerType = typeof(ConfigureRenderController);
});

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

WebApplication app = builder.Build();

await app.BootUmbracoAsync();
app.UseWebOptimizer();
app.UseWebMarkupMin();

app.UseHttpsRedirection();

app.UseCookiePolicy();
app.UseSession();

app.UseInitMiddleware();
//app.UseResponseCompression();

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


app.Use(async (context, next) =>
{
    //context.Response.Headers.Add("X-Xss-Protection", "1; mode=block");
    //context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000");
    //context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    //context.Response.Headers.Add("X-Frame-Options", "SAMEORIGIN");
    await next();
    if (context.Response.StatusCode == 404)
    {
        context.Request.Path = "/error";
        await next();
    }
});
await app.RunAsync();
