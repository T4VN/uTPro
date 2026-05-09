using uTPro.Project.Web.Configure;
using uTPro.Project.Web.Startup;

var builder = WebApplication.CreateBuilder(args);

// 1. Hosting (TMP/TEMP override for multi-site IIS)
builder.ConfigureTempPath();

// 2. Umbraco CMS
builder.ConfigureUmbraco();

// 3. Performance (Razor, WebOptimizer, WebMarkupMin, OutputCache, ResponseCache)
builder.Services.AddPerformanceServices(builder.Environment);

// 4. Security (DataProtection, Form/IIS/Kestrel limits)
builder.Services.AddSecurityServices(builder);

// 5. MVC + Rendering
builder.Services.AddRenderingDefaults();
builder.Services.AddControllers();
builder.Services.AddBlockPreviewServices();

// Build & Run
var app = builder.Build();

await app.BootUmbracoAsync();

app.ConfigurePipeline();

await app.RunAsync();
