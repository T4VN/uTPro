using uTPro.Common.Constants;
using uTPro.Project.Web.Configure;
using uTPro.Project.Web.Startup;

// Web root (uTPro:Hosting:RootPath) must be applied via options — it cannot be changed
// after the builder is created.
var setup = HostingSetup.BuildWebApplicationOptions(args);
var builder = WebApplication.CreateBuilder(setup);

// Expose the resolved web/content root to PathFolder (a DI-less static helper) so code
// that locates files via PathFolder respects the configured RootPath instead of assuming
// the default wwwroot under the process current directory.
PathFolder.WebRootPathOverride = builder.Environment.WebRootPath;
PathFolder.ContentRootPathOverride = builder.Environment.ContentRootPath;

// 1. Hosting (TMP/TEMP + media override for multi-site / multi-app deployments)
builder.ConfigureAppSettings();
builder.ConfigureTempPath();
builder.ConfigureMediaPath();

// 2. Umbraco CMS
builder.ConfigureUmbraco();

// 3. Performance (Razor, WebOptimizer, WebMarkupMin, OutputCache, ResponseCache)
builder.Services.AddPerformanceServices(builder.Environment);

// 4. Security (DataProtection, Form/IIS/Kestrel limits)
builder.Services.AddSecurityServices(builder);

// 4b. Forwarded headers (real client IP/scheme behind reverse proxy / load balancer)
builder.Services.AddForwardedHeadersConfig(builder);

// 5. MVC + Rendering
builder.Services.AddRenderingDefaults();
builder.Services.AddControllers();
builder.Services.AddBlockPreviewServices();

// Build & Run
var app = builder.Build();

await app.BootUmbracoAsync();

app.ConfigurePipeline();

await app.RunAsync();
