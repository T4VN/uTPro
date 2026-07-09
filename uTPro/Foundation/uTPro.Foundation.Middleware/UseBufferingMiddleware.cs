using Microsoft.AspNetCore.Builder;

namespace uTPro.Foundation.Middleware
{
    public static class UseBufferingMiddleware
    {
        public static IApplicationBuilder UseBuffering(this IApplicationBuilder app)
        {
            return app.UseMiddleware<BufferingMiddleware>();
        }
    }
}
