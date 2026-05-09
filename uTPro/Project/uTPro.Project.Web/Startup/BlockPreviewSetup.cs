using Microsoft.AspNetCore.Mvc.Razor;
using Umbraco.Community.BlockPreview.Interfaces;
using uTPro.Project.Web.Configure;

namespace uTPro.Project.Web.Startup;

/// <summary>
/// Block Preview custom view location and service registration.
/// </summary>
public static class BlockPreviewSetup
{
    public static IServiceCollection AddBlockPreviewServices(this IServiceCollection services)
    {
        services.Configure<RazorViewEngineOptions>(options =>
        {
            options.ViewLocationExpanders.Add(new CustomBlockPreviewLocationExpander());
        });

        services.AddTransient<IBlockPreviewService, CustomBlockPreviewService>();

        return services;
    }
}
