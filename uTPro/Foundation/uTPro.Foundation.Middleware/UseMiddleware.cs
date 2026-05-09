using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace uTPro.Foundation.Middleware
{
    public static class UseMiddleware
    {
        /// <summary>
        /// Wires the uTPro request pipeline additions.
        /// Buffering is NOT enabled globally: copying every request body into a
        /// MemoryStream on every request is wasteful and only needed for endpoints
        /// that re-read the body (typically /umbraco backoffice). Callers that need
        /// it can invoke <see cref="UseBufferingMiddleware.UseBuffering"/> selectively.
        /// </summary>
        public static IApplicationBuilder UseInitMiddleware(this IApplicationBuilder app)
        {
            ArgumentNullException.ThrowIfNull(app);

            app.UseWebRequestLocalization();
            return app;
        }
    }
}
