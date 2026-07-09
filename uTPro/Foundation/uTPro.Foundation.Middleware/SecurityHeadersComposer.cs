using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;

namespace uTPro.Foundation.Middleware
{
    /// <summary>
    /// Self-registers <see cref="SecurityHeadersMiddleware"/> and its per-site resolver
    /// (<see cref="ISecurityHeadersFoundation"/>) into the Umbraco request pipeline without
    /// editing Startup. The resolver reads the per-site <c>GlobalSecurityHeadersSettings</c>
    /// backoffice node (no appsettings dependency). The middleware runs after routing so
    /// frontend HTML responses can emit the headers.
    /// </summary>
    public sealed class SecurityHeadersComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Per-site resolver: reads the GlobalSecurityHeadersSettings backoffice node.
            builder.Services.AddScoped<ISecurityHeadersFoundation, SecurityHeadersFoundation>();

            builder.Services.Configure<UmbracoPipelineOptions>(options =>
                options.AddFilter(new UmbracoPipelineFilter(nameof(SecurityHeadersComposer))
                {
                    // PostRouting (not PostPipeline): the middleware must wrap endpoint execution
                    // so its Response.OnStarting callback fires before the frontend HTML response
                    // is flushed. Middleware added after UseEndpoints would not run for
                    // endpoint-handled page requests.
                    PostRouting = app => app.UseSecurityHeaders()
                }));
        }
    }
}
