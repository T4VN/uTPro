using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace uTPro.Feature.Dashboard;

/// <summary>
/// Registers the uTPro Dashboard's backoffice wiring. Currently registers a dedicated Swagger
/// document so the Dashboard API shows as its own entry in the Swagger UI "Select a definition"
/// dropdown.
/// </summary>
public sealed class DashboardComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.ConfigureOptions<ConfigureDashboardSwaggerGenOptions>();
    }
}
