using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;

namespace uTPro.Extension
{
    /// <summary>
    /// Ensures <see cref="IHttpContextAccessor"/> is registered (Umbraco already
    /// registers it but we keep this defensive) and wires it into
    /// <see cref="HttpContextStatic.Accessor"/> so static extension methods can
    /// participate in per-request caching.
    /// </summary>
    public class HttpContextStaticComposer : IComposer
    {
        /// <summary>
        /// Composes the Umbraco application by registering <see cref="IHttpContextAccessor"/>
        /// and wiring it into the application startup pipeline via <see cref="HttpContextStaticStartupFilter"/>.
        /// </summary>
        /// <param name="builder">The Umbraco builder used to register services.</param>
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddHttpContextAccessor();

            // Runs after the service provider is built to capture the accessor.
            builder.Services.AddSingleton<IStartupFilter, HttpContextStaticStartupFilter>();
        }
    }
}