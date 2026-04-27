using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using uTPro.Feature.UrlViewer.Services;

namespace uTPro.Feature.UrlViewer;

/// <summary>
/// Registers UrlViewer services into the DI container via Umbraco Composer pattern.
/// </summary>
public class UrlViewerComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Register HttpClient for UrlViewer
        builder.Services.AddHttpClient("UrlViewer")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 10,
                AutomaticDecompression = System.Net.DecompressionMethods.All
            });

        builder.Services.AddScoped<IUrlViewerService, UrlViewerService>();
    }
}
