using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace uTPro.Extension
{
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
