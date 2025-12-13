using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace uTPro.Foundation.Middleware
{
    public static class UseBufferingMiddleware
    {
        public static IApplicationBuilder UseBuffering(this IApplicationBuilder app)
        {
            return app.UseMiddleware<BufferingMiddleware>();
        }
    }

    internal class BufferingMiddleware
    {
        private readonly RequestDelegate _next;
        public BufferingMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            context.Request.EnableBuffering();
            await _next(context);
        }
    }
}
