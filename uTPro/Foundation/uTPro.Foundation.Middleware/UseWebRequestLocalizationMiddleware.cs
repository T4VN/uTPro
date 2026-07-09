using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using uTPro.Common.Constants;

namespace uTPro.Foundation.Middleware
{
    public static class UseWebRequestLocalizationMiddleware
    {
        public static IApplicationBuilder UseWebRequestLocalization(this IApplicationBuilder app)
        {
            var config = app.ApplicationServices.GetRequiredService<IConfiguration>();
            bool.TryParse(config.GetSection(ConfigSettingUTPro.ListRememberLanguage.Enabled)?.Value, out bool isEnabled);
            if (isEnabled)
            {
                var requestLocalizationOptions = app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>();
                app.UseRequestLocalization(requestLocalizationOptions.Value);
                return app.UseMiddleware<RequestLocalizationOptionMiddleware>();
            }
            return app;
        }
    }
}
