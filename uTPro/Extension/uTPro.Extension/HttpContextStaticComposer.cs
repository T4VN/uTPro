using Microsoft.AspNetCore.Builder;
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
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddHttpContextAccessor();

            // Runs after the service provider is built to capture the accessor.
            builder.Services.AddSingleton<IStartupFilter, HttpContextStaticStartupFilter>();
        }
    }

    internal sealed class HttpContextStaticStartupFilter : IStartupFilter
    {
        private readonly IHttpContextAccessor _accessor;

        public HttpContextStaticStartupFilter(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
            HttpContextStatic.Accessor = accessor;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            // Re-assign defensively in case something reset it. Cheap no-op otherwise.
            HttpContextStatic.Accessor = _accessor;
            return next;
        }
    }
}