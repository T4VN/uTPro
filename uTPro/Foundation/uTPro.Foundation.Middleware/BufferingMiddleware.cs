using Microsoft.AspNetCore.Http;

namespace uTPro.Foundation.Middleware
{
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
