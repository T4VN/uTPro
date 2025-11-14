using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Globalization;
using System.IO;
using Umbraco.Cms.Core.Media.EmbedProviders;
using uTPro.Common.Constants;
using uTPro.Extension;
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
        private static readonly DateTimeOffset exp_Cookie = DateTimeOffset.UtcNow.AddDays(3);
        private static readonly Lazy<HashSet<string>> _wwwRootEntries = new(() =>
        {
            try
            {
                var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                result.Add("error");
                result.Add("robots");
                result.Add("robots.txt");
                result.Add("sitemap");
                result.Add("sitemap.xml");
                result.Add("favicon");
                result.Add("favicon.ico");
                //folder
                var folderROOT = PathFolder.DirectoryWWWRoot;
                if (Directory.Exists(folderROOT))
                {
                    var lst = Directory.GetDirectories(folderROOT).Concat(Directory.GetFiles(folderROOT));
                    foreach (var item in lst)
                    {
                        result.Add(Path.GetFileName(item));
                    }
                }
                return result;
            }
            catch
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        });

        private IEnumerable<Umbraco.Cms.Core.Routing.Domain> domains = Enumerable.Empty<Umbraco.Cms.Core.Routing.Domain>();
        RequestDelegate _next;
        ICurrentSiteExtension _currentSite;
        ILogger<RequestLocalizationOptionMiddleware> _logger;

        public RequestLocalizationOptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentSiteExtension currentSite, ILogger<RequestLocalizationOptionMiddleware> logger)
        {
            if (context == null)
            {
                _logger.LogError($"Error in RequestLocalizationOptionMiddleware: Context is null");
                throw new ArgumentNullException(nameof(context));
            }
            else
            {
                _logger = logger;
                _currentSite = currentSite;
                if (_currentSite?.Configuration == null) return;
                try
                {
                    if (!_currentSite.GetItem().FolderSettings.RememberLanguageDisable)
                    {
                        string fullUrl = await DetermineProviderCultureResult(context).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(fullUrl))
                        {
                            context.Response.Redirect(fullUrl, true);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in RequestLocalizationOptionMiddleware: {ex.Message}");
                }
            }
            await _next.Invoke(context).ConfigureAwait(false);
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

            (string culture, string prefixUrl, bool isRedirect) = await GetUrlCulture(context, parts).ConfigureAwait(false);

            if (SetGlobal(context, culture))
            {
                return GetSchemeRedirect(context, culture, prefixUrl, isRedirect);
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

                //Folders and files in wwwroot (cached to avoid IO on every request)
                foreach (var item in _wwwRootEntries.Value)
                {
                    yield return item;
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
            if (httpContext == null) return;

            if (!string.Equals(httpContext.Request.Cookies[cookie_Culture], culture, StringComparison.Ordinal))
            {
                httpContext.Response.Cookies.Append(cookie_Culture, culture, new CookieOptions
                {
                    Expires = exp_Cookie,
                    IsEssential = true,
                    HttpOnly = true,
                    Secure = httpContext.Request.IsHttps
                }
                );
            }
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
                    if (context != null)
                        StoreCookie(context, culture);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SetGlobal culture error: {ex.Message}");
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

        private string GetSchemeRedirect(HttpContext? httpContext, string culture, string prefixUrl, bool isRedirect)
        {
            if (!isRedirect || httpContext == null)
            {
                return string.Empty;
            }

            prefixUrl = SchemeUrlExtensions.AddScheme(prefixUrl);

            if (!string.IsNullOrEmpty(prefixUrl) && Uri.TryCreate(prefixUrl, UriKind.RelativeOrAbsolute, out var redirectUri))
            {
                foreach (var item in domains)
                {
                    if (Uri.TryCreate(SchemeUrlExtensions.AddScheme(item.Name), UriKind.RelativeOrAbsolute, out var domainHost))
                    {
                        if (string.Equals(domainHost.Host, redirectUri.Host, StringComparison.OrdinalIgnoreCase))
                        {
                            return prefixUrl + httpContext.Request.Path + httpContext.Request.QueryString;
                        }
                    }
                }
            }
            return string.Empty;
        }


        private async Task<Tuple<string, string, bool>> GetUrlCulture(HttpContext? context, string[] parts)
        {
            Umbraco.Cms.Core.Routing.Domain? cul = null;
            domains = await _currentSite.GetDomains(false).ConfigureAwait(false);
            bool isRedirect = true;
            string culture = string.Empty;
            if (parts.Length > 0)//parts >0
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
            return Tuple.Create(cul?.Culture ?? string.Empty, cul?.Name ?? string.Empty, isRedirect);
        }
    }
}
