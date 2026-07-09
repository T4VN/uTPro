using Microsoft.AspNetCore.Builder;

namespace uTPro.Foundation.Middleware
{
    public static class UseSecurityHeadersMiddleware
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
