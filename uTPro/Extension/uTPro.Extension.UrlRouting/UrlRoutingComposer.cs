using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace uTPro.Extension.UrlRouting;

/// <summary>
/// Registers all URL routing services: hidden containers, transparent routing, and category routing.
/// </summary>
public sealed class UrlRoutingComposer : IComposer
{
    /// <summary>
    /// Registers URL-routing configuration, services, URL providers, and content finders with Umbraco.
    /// </summary>
    /// <param name="builder">The Umbraco builder used to configure the application.</param>
    public void Compose(IUmbracoBuilder builder)
    {
        // Configuration
        builder.Services.Configure<HiddenUrlOptions>(builder.Config.GetSection(HiddenUrlOptions.SectionName));

        // Shared services
        builder.Services.TryAddSingleton<HiddenContainerAliases>();
        builder.Services.TryAddSingleton<CategoryUrlService>();
        builder.Services.TryAddSingleton<ContentTreeWalker>();

        // Hidden URL provider — suppress URLs for container nodes
        builder.UrlProviders().Insert<HiddenUrlProvider>();

        // Backoffice "Links" panel decorator
        builder.Services.AddTransient<PublishedUrlInfoProvider>();
        builder.Services.Replace(ServiceDescriptor.Transient<IPublishedUrlInfoProvider>(sp =>
            new HiddenContainerUrlInfoProvider(
                sp.GetRequiredService<PublishedUrlInfoProvider>(),
                sp.GetRequiredService<HiddenContainerAliases>(),
                sp.GetRequiredService<IUmbracoContextAccessor>(),
                sp.GetRequiredService<IDocumentUrlService>())));

        // Examine index exclusion for hidden containers
        builder.Services.ConfigureOptions<HiddenContainerIndexOptions>();

        // Transparent container URL provider
        builder.UrlProviders().Insert<TransparentContainerUrlProvider>();

        // Category URL provider (before transparent so category URLs take precedence)
        builder.UrlProviders().Insert<CategoryUrlProvider>();

        // Content finders (in order: transparent → category → category landing)
        builder.ContentFinders().Append<TransparentContainerContentFinder>();
        builder.ContentFinders().Append<CategoryUrlContentFinder>();
        builder.ContentFinders().Append<CategoryLandingContentFinder>();
    }
}
