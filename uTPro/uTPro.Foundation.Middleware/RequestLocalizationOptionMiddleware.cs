﻿using Microsoft.AspNetCore.Builder;
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
            var requestLocalizationOptions = app.ApplicationServices.GetRequiredService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(requestLocalizationOptions.Value);
            return app.UseMiddleware<RequestLocalizationOptionMiddleware>();
        }
    }

    internal class RequestLocalizationOptionMiddleware
    {

        private const string cookie_Culture = ".AspNetCore.Culture";
        private readonly DateTime exp_Cookie = DateTime.Now.AddDays(3);

        RequestDelegate _next;
        HttpContext _httpContext;
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
                _httpContext = context;
                _currentSite = currentSite;
                try
                {
                    string url = await DetermineProviderCultureResult();
                    if (!string.IsNullOrEmpty(url))
                    {
                        context.Response.Redirect(url, true);
                        return;
                    }
                }
                catch (Exception)
                {

                }
            }
            await _next.Invoke(context);
        }

        private async Task<string> DetermineProviderCultureResult()
        {
            string[] parts = (_httpContext.Request?.Path.Value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (IsExludeHost(_httpContext.Request?.Host))
            {
                return string.Empty;
            }
            if (IsExludePathUrl(parts))
            {
                return string.Empty;
            }

            (string culture, string urlRedirect, bool isRedirect) = await GetUrlCulture(parts);

            if (SetGlobal(culture))
            {
                return GetUrlRedirect(_httpContext, culture, urlRedirect, isRedirect);
            }
            return string.Empty;
        }

        private bool IsExludeHost(HostString? host)
        {
            if (!host.HasValue)
            {
                return false;
            }
            if (_httpContext.Request.Path.StartsWithSegments("/umbraco"))
            {
                bool isEnableCheckBackoffice = false;
                bool.TryParse(_currentSite.Configuration.GetSection(ConfigSettingUTPro.Backoffice.Enabled)?.Value, out isEnableCheckBackoffice);
                if (isEnableCheckBackoffice)
                {
                    var lstUrl = _currentSite.Configuration.GetSection(ConfigSettingUTPro.Backoffice.Url)?.Value?
                        .Split(new List<string> { ",", ";" }.ToArray(), StringSplitOptions.RemoveEmptyEntries);

                    if (lstUrl == null || !lstUrl.Any())
                    {
                        return false;
                    }
                    return lstUrl.Any(x => x.Equals(host.Value.Host, StringComparison.OrdinalIgnoreCase));
                }
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
                bool.TryParse(_currentSite.Configuration.GetSection(ConfigSettingUTPro.ListExludeRequestLanguage.Enabled)?.Value, out isEnabled);
                if (isEnabled)
                {
                    var lstPaths = _currentSite.Configuration.GetSection(ConfigSettingUTPro.ListExludeRequestLanguage.Paths).Get<string[]>();
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

        private bool SetGlobal(string culture)
        {
            try
            {
                var cul = new CultureInfo(culture);
                _currentSite.SetCurrentCulture(cul);
                CultureInfo.DefaultThreadCurrentCulture = cul;
                CultureInfo.DefaultThreadCurrentUICulture = cul;
                Thread.CurrentThread.CurrentCulture = cul;
                Thread.CurrentThread.CurrentUICulture = cul;
                StoreCookie(_httpContext, culture);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string GetLanguageDefault() => _currentSite.DefaultCulture;

        private string GetUrlRedirect(HttpContext httpContext, string culture, string urlRedirect, bool isRedirect)
        {
            if (!isRedirect)
            {
                return string.Empty;
            }
            string urlPath = (urlRedirect + "/" + httpContext.Request.Path).Replace("//", "/");
            string url = urlRedirect;
            if (urlRedirect.StartsWith("/"))
            {
                url = httpContext.Request.Scheme + ":/" + urlPath;
            }
            else
            {
                url = httpContext.Request.Scheme + "://" + urlPath;
            }
            return url;
        }

        private async Task<Tuple<string, string, bool>> GetUrlCulture(string[] parts)
        {
            Umbraco.Cms.Core.Routing.Domain? cul = null;
            bool isRedirect = true;
            if (parts.Length > 0)//parts > 0
            {
                cul = (await _currentSite.GetDomains(false))?.FirstOrDefault(x => x.Name.Contains(parts[0]));
                if (cul != null)
                {
                    isRedirect = false;
                }
            }
            else//root url
            {
                //Get Cookie
                string culture = _httpContext?.Request?.Cookies[cookie_Culture]?.ToString();
                if (string.IsNullOrWhiteSpace(culture))
                {
                    culture = GetLanguageDefault();
                }

                cul = (await _currentSite.GetDomains(false))?.FirstOrDefault(x => x.Culture == null ? false :
                                                                       (x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase)//set current
                                                                       || x.Culture.Equals(GetLanguageDefault(), StringComparison.OrdinalIgnoreCase)//set default => if not correct cookie
                                                                       )
                                                                       ) ?? null;
            }

            if (cul == null)
            {
                var domains = await _currentSite.GetDomains(false);
                cul = domains?.FirstOrDefault();
            }
            return Tuple.Create(cul?.Culture ?? string.Empty, cul?.Name ?? string.Empty, isRedirect);
        }

    }
}
