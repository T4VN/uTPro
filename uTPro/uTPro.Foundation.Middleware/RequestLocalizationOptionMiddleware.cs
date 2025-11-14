using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using System.Globalization;
using System.IO;
using uTPro.Common.Constants;
using uTPro.Extension.CurrentSite;

namespace uTPro.Foundation.Middleware
{
    public static class UseWebRequestLocalizationMiddleware
    {
        public static IApplicationBuilder UseWebRequestLocalization(this IApplicationBuilder app)
        {
            var config = app.ApplicationServices.GetRequiredService<IConfiguration>();
            bool isEnabled = true;
            bool.TryParse(config.GetSection(ConfigSettingUTPro.ListRememberLanguage.Enabled)?.Value, out isEnabled);
            if (isEnabled)
            {
                var requestLocalizationOptions = app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>();
                app.UseRequestLocalization(requestLocalizationOptions.Value);
                return app.UseMiddleware<RequestLocalizationOptionMiddleware>();
            }
            return app;
        }
    }

    internal class RequestLocalizationOptionMiddleware
    {

        private const string cookie_Culture = ".UTPro.Culture";
        private readonly DateTime exp_Cookie = DateTime.Now.AddDays(3);
        private IEnumerable<Umbraco.Cms.Core.Routing.Domain>? domains = null;
        RequestDelegate _next;
        ICurrentSiteExtension _currentSite;

        public RequestLocalizationOptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentSiteExtension currentSite)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            else
            {
                _currentSite = currentSite;
                if (_currentSite?.Configuration == null) return;
                try
                {
                    if (!_currentSite.GetItem().FolderSettings.RememberLanguageDisable)
                    {
                        string url = await DetermineProviderCultureResult(context);
                        if (!string.IsNullOrEmpty(url))
                        {
                            // Enhanced: Only allow redirect if URL is relative OR host (if present) is trusted
                            Uri redirectUri;
                            if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out redirectUri))
                            {
                                // Allow only relative URLs, or absolute URLs with a trusted host
                                if (!redirectUri.IsAbsoluteUri ||
                                    (domains != null && domains.Any(x => string.Equals(x.Name, redirectUri.Host, StringComparison.OrdinalIgnoreCase))))
                                {
                                    context.Response.Redirect(url, true);
                                    return;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
            await _next.Invoke(context);
        }

        private async Task<string> DetermineProviderCultureResult(HttpContext? context)
        {
            if (IsExludeHost(context, context?.Request?.Host))
            {
                return string.Empty;
            }
            string[] parts = (context?.Request?.Path.Value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (IsExludePathUrl(parts))
            {
                return string.Empty;
            }

            (string culture, string urlRedirect, bool isRedirect) = await GetUrlCulture(context, parts);

            if (SetGlobal(context, culture))
            {
                return GetUrlRedirect(context, culture, urlRedirect, isRedirect);
            }
            return string.Empty;
        }

        private bool IsExludeHost(HttpContext? context, HostString? host)
        {
            if (host?.HasValue == false)
            {
                return false;
            }

            bool isEnableCheckBackoffice = false;
            bool.TryParse(_currentSite.Configuration.GetSection(ConfigSettingUTPro.Backoffice.Enabled)?.Value, out isEnableCheckBackoffice);
            if (isEnableCheckBackoffice)
            {
                var lstUrl = _currentSite.Configuration.GetSection(ConfigSettingUTPro.Backoffice.Domain)?.Value?
                    .Split(new List<string> { ",", ";" }.ToArray(), StringSplitOptions.RemoveEmptyEntries);

                if (lstUrl == null || !lstUrl.Any())
                {
                    return false;
                }
                return lstUrl.Any(x => x.Equals(host.Value.Host, StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }

        private IEnumerable<string> LstExlude
        {
            get
            {
                //exclude paths
                yield return "error";

                yield return "robots";
                yield return "robots.txt";

                yield return "sitemap";
                yield return "sitemap.xml";

                yield return "favicon";
                yield return "favicon.ico";

                //Exclude request config setting
                bool isEnabled = false;
                bool.TryParse(_currentSite.Configuration.GetSection(ConfigSettingUTPro.ListRememberLanguage.ListExludeRequestLanguage.Enabled)?.Value, out isEnabled);
                if (isEnabled)
                {
                    var lstPaths = _currentSite.Configuration.GetSection(ConfigSettingUTPro.ListRememberLanguage.ListExludeRequestLanguage.Paths).Get<string[]>();
                    if (lstPaths != null)
                    {
                        foreach (var item in lstPaths)
                        {
                            if (!string.IsNullOrWhiteSpace(item))
                            {
                                yield return item.Trim().ToLowerInvariant();
                            }
                        }
                    }

                }

                //Folders and files in wwwroot
                var folderROOT = PathFolder.DirectoryWWWRoot;
                var lst = Directory.GetDirectories(folderROOT).Concat(Directory.GetFiles(folderROOT));

                foreach (var item in lst)
                {
                    yield return Path.GetFileName(item);
                }
            }
        }

        private bool IsExludePathUrl(string[] parts)
        {
            if (parts.Length > 0)
                return LstExlude.Contains(parts[0], StringComparer.OrdinalIgnoreCase);
            return false;
        }

        private void StoreCookie(HttpContext httpContext, string culture)
        {
            if (httpContext.Request.Cookies[cookie_Culture] != culture)
                httpContext.Response.Cookies.Append(cookie_Culture, culture, new CookieOptions
                {
                    Expires = exp_Cookie,
                    IsEssential = true,
                    HttpOnly = true,
                    Secure = true
                }
                );
        }

        private bool SetGlobal(HttpContext? context, string culture)
        {
            try
            {
                if (!string.IsNullOrEmpty(culture))
                {
                    var cul = new CultureInfo(culture);
                    _currentSite.SetCurrentCulture(cul);
                    CultureInfo.DefaultThreadCurrentCulture = cul;
                    CultureInfo.DefaultThreadCurrentUICulture = cul;
                    Thread.CurrentThread.CurrentCulture = cul;
                    Thread.CurrentThread.CurrentUICulture = cul;
                    StoreCookie(context, culture);
                    return true;
                }
            }
            catch (Exception ex)
            {
            }
            return false;
        }

        private string GetLanguageDefault(IEnumerable<Umbraco.Cms.Core.Routing.Domain>? domains)
        {
            if (domains != null)
            {
                var langDefault = domains.FirstOrDefault(x => x.Name.EndsWith("/"));//languge default with url /
                if (langDefault != null && !string.IsNullOrEmpty(langDefault.Culture))
                {
                    return langDefault.Culture;
                }
            }
            return _currentSite.DefaultCulture;//default with umbraco
        }

        private string GetUrlRedirect(HttpContext? httpContext, string culture, string urlRedirect, bool isRedirect)
        {
            if (!isRedirect || httpContext == null)
            {
                return string.Empty;
            }

            var request = httpContext.Request;

            // Path + Query
            var pathAndQuery = $"{request.Path}{request.QueryString}";

            // If urlRedirect have scheme (http/https)
            var scheme = (urlRedirect.StartsWith("http")) ? string.Empty : $"https://";
            return $"{scheme}{urlRedirect}{pathAndQuery}";
        }

        private async Task<Tuple<string, string, bool>> GetUrlCulture(HttpContext? context, string[] parts)
        {
            Umbraco.Cms.Core.Routing.Domain? cul = null;
            domains = await _currentSite.GetDomains(false);
            bool isRedirect = true;
            string culture = string.Empty;
            if (parts.Length > 0)//parts > 0
            {
                cul = domains?.FirstOrDefault(x => x.Name.Contains(parts[0]));
                if (cul != null)
                {
                    isRedirect = false;
                }
            }
            else//root url
            {
                //Get Cookie
                culture = context?.Request?.Cookies[cookie_Culture]?.ToString() ?? string.Empty;
            }

            if (cul == null)
            {
                if (string.IsNullOrWhiteSpace(culture))
                {
                    culture = GetLanguageDefault(domains);
                }

                cul = domains?.FirstOrDefault(x => x.Culture == null ? false : x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase)) ?? null;
            }
            if (cul != null && cul.Name.IndexOf(context?.Request?.Host.Value ?? string.Empty) != 0)
            {
                isRedirect = false;
            }
            return Tuple.Create(cul?.Culture ?? string.Empty, cul?.Name ?? string.Empty, isRedirect);
        }

    }
}
