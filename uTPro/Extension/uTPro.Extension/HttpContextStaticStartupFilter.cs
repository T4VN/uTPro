using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace uTPro.Extension
{
    internal sealed class HttpContextStaticStartupFilter : IStartupFilter
    {
        private readonly IHttpContextAccessor _accessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpContextStaticStartupFilter"/> class
        /// and assigns the provided <paramref name="accessor"/> to <see cref="HttpContextStatic.Accessor"/>.
        /// </summary>
        /// <param name="accessor">The HTTP context accessor to be stored and used throughout the application.</param>
        public HttpContextStaticStartupFilter(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
            HttpContextStatic.Accessor = accessor;
        }

        /// <summary>
        /// Configures the application builder pipeline, ensuring the HTTP context accessor
        /// is assigned to <see cref="HttpContextStatic.Accessor"/> before proceeding with
        /// the next configuration action.
        /// </summary>
        /// <param name="next">The next configuration action in the pipeline.</param>
        /// <returns>The configuration action to be executed.</returns>
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            // Re-assign defensively in case something reset it. Cheap no-op otherwise.
            HttpContextStatic.Accessor = _accessor;
            return next;
        }
    }
}
