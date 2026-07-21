using Microsoft.Extensions.DependencyInjection; // SwaggerDoc(...) extension lives here
using Microsoft.Extensions.Options;
using Microsoft.OpenApi; // net10 / Umbraco 17+ uses Microsoft.OpenApi 2.x (flattened namespace)
using Swashbuckle.AspNetCore.SwaggerGen;

namespace uTPro.Feature.Dashboard;

/// <summary>
/// Registers a dedicated Swagger document for the uTPro Dashboard API so it appears as its own
/// entry in the backoffice Swagger UI "Select a definition" dropdown. Paired with
/// <c>[MapToApi(ApiName)]</c> on the controller and registered from <see cref="DashboardComposer"/>.
/// </summary>
public class ConfigureDashboardSwaggerGenOptions : IConfigureOptions<SwaggerGenOptions>
{
    /// <summary>The Swagger document name. Must match the value passed to <c>[MapToApi]</c>.</summary>
    public const string ApiName = "utpro-dashboard";

    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc(ApiName, new OpenApiInfo
        {
            Title = "uTPro Dashboard API",
            Version = "1.0",
            Description = "Dashboard stats, activity, audit-trail and version endpoints for the uTPro backoffice.",
        });
    }
}
