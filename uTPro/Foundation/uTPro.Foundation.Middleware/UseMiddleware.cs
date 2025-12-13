using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace uTPro.Foundation.Middleware
{
    public static class UseMiddleware
    {
        public static IApplicationBuilder UseInitMiddleware(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseWebRequestLocalization();
            app.UseBuffering();
            return app;
        }
    }
}
