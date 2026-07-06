using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
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

    internal class RequestLocalizationOptionMiddleware
    {
        private const string CookieCulture = ".uTPro.Culture";
        private static readonly DateTimeOffset CookieExpiry = DateTimeOffset.UtcNow.AddDays(3);

        private static readonly Lazy<HashSet<string>> _wwwRootEntries = new(() =>
        {
            try
            {
                var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "error", "robots", "robots.txt", "sitemap",
                    "sitemap.xml", "favicon", "favicon.ico"
                };
                var folderROOT = PathFolder.DirectoryWWWRoot;
                if (Directory.Exists(folderROOT))
                {
                    foreach (var item in Directory.GetDirectories(folderROOT).Concat(Directory.GetFiles(folderROOT)))
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

        // Cached exclude paths from config (built once per app lifetime).
        private static HashSet<string>? _cachedExcludePaths;
        private static readonly Lock _excludePathsLock = new();

        private readonly RequestDelegate _next;

        public RequestLocalizationOptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentSiteExtension currentSite, ILogger<RequestLocalizationOptionMiddleware> logger)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (currentSite?.Configuration == null)
            {
                await _next.Invoke(context).ConfigureAwait(false);
                return;
            }

            try
            {
                bool.TryParse(currentSite.Configuration.GetSection(ConfigSettingUTPro.Backoffice.Enabled)?.Value, out bool isEnableCheckBackoffice);

                string fullUrl = DetermineProviderCultureResult(context, currentSite, isEnableCheckBackoffice);
                if (!string.IsNullOrEmpty(fullUrl) && IsLocalUrl(fullUrl))
                {
                    context.Response.Redirect(fullUrl, true);
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in RequestLocalizationOptionMiddleware: {Message}", ex.Message);
            }

            await _next.Invoke(context).ConfigureAwait(false);
        }

        private static string DetermineProviderCultureResult(HttpContext context, ICurrentSiteExtension currentSite, bool isEnableCheckBackoffice)
        {
            if (IsExcludeHost(context, currentSite, isEnableCheckBackoffice))
                return string.Empty;

            string[] parts = (context.Request.Path.Value ?? string.Empty)
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (IsExcludePathUrl(parts, currentSite, isEnableCheckBackoffice))
                return string.Empty;

            if (currentSite.GetItem()?.FolderSettings?.RememberLanguageDisable == false)
            {
                var domains = currentSite.GetDomains(false);
                var (culture, prefixUrl, isRedirect) = GetUrlCulture(context, parts, domains, currentSite);

                if (SetGlobal(context, culture, currentSite))
                {
                    return GetSchemeRedirect(context, prefixUrl, isRedirect, domains);
                }
            }
            return string.Empty;
        }

        private static bool IsExcludeHost(HttpContext context, ICurrentSiteExtension currentSite, bool isEnableCheckBackoffice)
        {
            if (!context.Request.Host.HasValue)
                return false;

            if (!isEnableCheckBackoffice)
                return false;

            var lstUrl = currentSite.Configuration.GetSection(ConfigSettingUTPro.Backoffice.Domain)?.Value?
                .Split([",", ";"], StringSplitOptions.RemoveEmptyEntries);

            if (lstUrl == null || lstUrl.Length == 0)
                return false;

            var host = context.Request.Host.Host;
            return lstUrl.Any(x => x.Trim().Equals(host, StringComparison.OrdinalIgnoreCase));
        }

        private static HashSet<string> GetExcludePaths(ICurrentSiteExtension currentSite, bool isEnableCheckBackoffice)
        {
            if (_cachedExcludePaths != null)
                return _cachedExcludePaths;

            lock (_excludePathsLock)
            {
                if (_cachedExcludePaths != null)
                    return _cachedExcludePaths;

                var result = new HashSet<string>(_wwwRootEntries.Value, StringComparer.OrdinalIgnoreCase);

                if (!isEnableCheckBackoffice)
                {
                    result.Add("umbraco");
                    result.Add("app_plugins");
                }

                bool isEnabled = false;
                var configSection = currentSite.Configuration.GetSection(ConfigSettingUTPro.ListRememberLanguage.ListExludeRequestLanguage.Enabled);
                if (configSection != null && bool.TryParse(configSection.Value, out isEnabled) && isEnabled)
                {
                    var pathsSection = currentSite.Configuration.GetSection(ConfigSettingUTPro.ListRememberLanguage.ListExludeRequestLanguage.Paths);
                    var lstPaths = pathsSection?.Get<string[]>();
                    if (lstPaths != null)
                    {
                        foreach (var item in lstPaths)
                        {
                            if (!string.IsNullOrWhiteSpace(item))
                                result.Add(item.Trim().ToLowerInvariant());
                        }
                    }
                }

                _cachedExcludePaths = result;
                return _cachedExcludePaths;
            }
        }

        private static bool IsExcludePathUrl(string[] parts, ICurrentSiteExtension currentSite, bool isEnableCheckBackoffice)
        {
            if (parts.Length == 0) return false;
            return GetExcludePaths(currentSite, isEnableCheckBackoffice).Contains(parts[0]);
        }

        private static void StoreCookie(HttpContext httpContext, string culture)
        {
            if (string.Equals(httpContext.Request.Cookies[CookieCulture], culture, StringComparison.Ordinal))
                return;

            httpContext.Response.Cookies.Append(CookieCulture, culture, new CookieOptions
            {
                Expires = CookieExpiry,
                IsEssential = true,
                HttpOnly = true,
                Secure = httpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/"
            });
        }

        private static bool SetGlobal(HttpContext context, string culture, ICurrentSiteExtension currentSite)
        {
            if (string.IsNullOrEmpty(culture))
                return false;

            try
            {
                var cul = new CultureInfo(culture);
                currentSite.SetCurrentCulture(cul);
                CultureInfo.DefaultThreadCurrentCulture = cul;
                CultureInfo.DefaultThreadCurrentUICulture = cul;
                Thread.CurrentThread.CurrentCulture = cul;
                Thread.CurrentThread.CurrentUICulture = cul;
                StoreCookie(context, culture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GetLanguageDefault(IReadOnlyList<Umbraco.Cms.Core.Routing.Domain> domains, ICurrentSiteExtension currentSite)
        {
            var langDefault = domains.FirstOrDefault(x => x.Name.EndsWith("/"));
            if (langDefault != null && !string.IsNullOrEmpty(langDefault.Culture))
                return langDefault.Culture;

            return currentSite.DefaultCulture;
        }

        private static string GetSchemeRedirect(HttpContext httpContext, string prefixUrl, bool isRedirect, IReadOnlyList<Umbraco.Cms.Core.Routing.Domain> domains)
        {
            if (!isRedirect) return string.Empty;

            string _prefixUrl = SchemeUrlExtensions.AddScheme(prefixUrl);

            if (!string.IsNullOrEmpty(_prefixUrl) && Uri.TryCreate(_prefixUrl, UriKind.RelativeOrAbsolute, out var redirectUri))
            {
                foreach (var item in domains)
                {
                    if (Uri.TryCreate(SchemeUrlExtensions.AddScheme(item.Name), UriKind.RelativeOrAbsolute, out var domainHost))
                    {
                        if (string.Equals(domainHost.Host, redirectUri.Host, StringComparison.OrdinalIgnoreCase))
                        {
                            var prefixPath = redirectUri.AbsolutePath ?? string.Empty;
                            if (prefixPath.Contains("://", StringComparison.Ordinal))
                                return string.Empty;

                            prefixPath = "/" + prefixPath.Trim('/');
                            if (prefixPath == "/")
                                prefixPath = string.Empty;

                            var currentPath = httpContext.Request.Path.Value ?? "/";
                            var query = httpContext.Request.QueryString.ToString();

                            // Build the target path, avoiding a double slash when the request
                            // is at the site root ("/") but a language prefix is being added.
                            string target;
                            if (string.IsNullOrEmpty(prefixPath))
                                target = currentPath + query;
                            else if (currentPath == "/")
                                target = prefixPath + query;
                            else
                                target = prefixPath + currentPath + query;

                            // Never redirect to the URL we are already on (prevents redirect loops,
                            // e.g. the default culture served at the site root).
                            if (string.Equals(target, currentPath + query, StringComparison.Ordinal))
                                return string.Empty;

                            return target;
                        }
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Validates that the redirect URL is a local URL (same-origin) to prevent
        /// open-redirect / phishing attacks (CWE-601). Rejects absolute URLs,
        /// protocol-relative URLs (e.g. "//evil.com"), backslash tricks, and any
        /// URL containing a scheme delimiter.
        /// </summary>
        private static bool IsLocalUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            // Reject protocol-relative URLs: "//host" or "/\host"
            if (url.Length > 1 && (url[0] == '/' || url[0] == '\\')
                && (url[1] == '/' || url[1] == '\\'))
                return false;

            // Reject absolute URLs with a scheme (e.g. "http://", "javascript:")
            if (url.Contains("://", StringComparison.Ordinal))
                return false;

            // Must be rooted-relative (start with '/')
            if (url[0] != '/')
                return false;

            // Parse as relative URI to ensure no host component sneaks in
            if (!Uri.TryCreate(url, UriKind.Relative, out _))
                return false;

            return true;
        }

        private static (string culture, string prefixUrl, bool isRedirect) GetUrlCulture(
            HttpContext context, string[] parts,
            IReadOnlyList<Umbraco.Cms.Core.Routing.Domain> domains,
            ICurrentSiteExtension currentSite)
        {
            Umbraco.Cms.Core.Routing.Domain? cul = null;
            bool isRedirect = true;
            string culture = string.Empty;

            if (parts.Length > 0)
            {
                // Match domain by segment (use Equals on trimmed name for accuracy)
                cul = domains.FirstOrDefault(x =>
                    x.Name.Trim('/').Equals(parts[0], StringComparison.OrdinalIgnoreCase)
                    || x.Name.Contains(parts[0], StringComparison.OrdinalIgnoreCase));
                if (cul != null)
                    isRedirect = false;
            }
            else
            {
                // Root URL — try cookie
                culture = context.Request.Cookies[CookieCulture]?.ToString() ?? string.Empty;
            }

            if (cul == null)
            {
                if (string.IsNullOrWhiteSpace(culture))
                    culture = GetLanguageDefault(domains, currentSite);

                cul = SelectDomainForCulture(domains, culture);
            }

            return (cul?.Culture ?? string.Empty, cul?.Name ?? string.Empty, isRedirect);
        }

        private static Umbraco.Cms.Core.Routing.Domain? SelectDomainForCulture(
            IReadOnlyList<Umbraco.Cms.Core.Routing.Domain> domains, string culture)
        {
            if (string.IsNullOrEmpty(culture))
                return null;

            var candidates = domains
                .Where(x => x.Culture != null
                    && x.Culture.Equals(culture, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (candidates.Count == 0)
                return null;
            if (candidates.Count == 1)
                return candidates[0];

            var distinctHosts = candidates
                .Select(GetDomainHost)
                .Where(h => !string.IsNullOrEmpty(h))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            // Same host across candidates → prefer the entry with a path segment (/vi).
            if (distinctHosts <= 1)
                return candidates.FirstOrDefault(HasPathSegment) ?? candidates[0];

            // Different hosts → keep the configured domain list order.
            return candidates[0];
        }

        private static string GetDomainHost(Umbraco.Cms.Core.Routing.Domain domain)
        {
            if (Uri.TryCreate(SchemeUrlExtensions.AddScheme(domain.Name), UriKind.RelativeOrAbsolute, out var uri)
                && uri.IsAbsoluteUri)
                return uri.Host;
            return string.Empty;
        }

        private static bool HasPathSegment(Umbraco.Cms.Core.Routing.Domain domain)
        {
            if (Uri.TryCreate(SchemeUrlExtensions.AddScheme(domain.Name), UriKind.RelativeOrAbsolute, out var uri)
                && uri.IsAbsoluteUri)
                return !string.IsNullOrEmpty(uri.AbsolutePath.Trim('/'));

            return !string.IsNullOrEmpty(domain.Name.Trim('/'));
        }
    }
}
