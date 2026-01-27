using Microsoft.AspNetCore.Mvc.Razor;
using System.Collections.Concurrent;
using uTPro.Common.Constants;

namespace uTPro.Project.Web.Configure
{
    public sealed class CustomBlockPreviewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context)
        {
        }

        public IEnumerable<string> ExpandViewLocations(
            ViewLocationExpanderContext context,
            IEnumerable<string> viewLocations)
        {
            var viewName = context?.ViewName ?? string.Empty;

            // Try to get a cached custom location. If one exists and it's not a duplicate, yield it.
            var custom = CustomPathViews.GetPathViewBlockPreview(viewName, isCheckSiteName: false);
            if (!string.IsNullOrEmpty(custom) && !viewLocations.Contains(custom, StringComparer.OrdinalIgnoreCase))
            {
                yield return custom;
            }

            foreach (var location in viewLocations)
            {
                yield return location;
            }
        }
    }

    public static class CustomPathViews
    {
        // Cache computed paths to avoid repeated parsing for the same view name
        private static readonly ConcurrentDictionary<string, string?> s_pathCache = new(StringComparer.OrdinalIgnoreCase);

        public static string? GetPathViewBlockPreview(string viewName, string siteName = "", bool isCheckSiteName = true)
        {
            if (string.IsNullOrEmpty(viewName))
                return null;

            var cacheKey = string.Concat(viewName, "|", siteName ?? string.Empty, "|", isCheckSiteName ? "1" : "0");
            if (s_pathCache.TryGetValue(cacheKey, out var cached))
            {
                if (cached != null)
                {
                    return cached;
                }
            }

            var (site, fileName) = GetSiteAndFileName(viewName, siteName ?? string.Empty, isCheckSiteName);
            string result = viewName;

            if (!string.IsNullOrEmpty(site) && !string.IsNullOrEmpty(fileName))
            {
                // Basic sanitization: remove leading slashes and keep only filename part to avoid traversal
                fileName = fileName.TrimStart('/', '\\');
                if (fileName.IndexOfAny(new[] { '\\', '/' }) >= 0)
                {
                    fileName = Path.GetFileName(fileName);
                }

                if (!string.IsNullOrEmpty(fileName))
                {
                    result = $"~/Views/{site}/blockgrid/Components/{fileName}.cshtml";
                }
            }

            s_pathCache.TryAdd(cacheKey, result);
            return result;
        }

        public static (string, string) GetSiteAndFileName(string viewName, string siteName = "", bool isCheckSiteName = true)
        {
            if (string.IsNullOrEmpty(viewName))
                return (string.Empty, string.Empty);

            // special-case for global layout name (preserve behavior: only return when siteName provided)
            if (viewName.Equals("globalLayout", StringComparison.OrdinalIgnoreCase))
            {
                return (siteName ?? string.Empty, "_Layout");
            }

            // Short-circuit: when checking site name is required but none provided, match original behavior
            if (isCheckSiteName && string.IsNullOrEmpty(siteName))
            {
                return (string.Empty, viewName);
            }

            // Try to parse pattern: "{site}{Prefix.PrefixData}{fileName}" where Prefix.PrefixData = "__"
            var prefix = Prefix.PrefixData;
            var idx = viewName.IndexOf(prefix, StringComparison.Ordinal);
            if (idx > 0)
            {
                var parsedSite = viewName.Substring(0, idx);
                var parsedFile = idx + prefix.Length < viewName.Length ? viewName.Substring(idx + prefix.Length) : string.Empty;

                if (isCheckSiteName)
                {
                    if (!string.IsNullOrEmpty(siteName) && string.Equals(parsedSite, siteName, StringComparison.OrdinalIgnoreCase))
                    {
                        return (siteName, parsedFile);
                    }
                    return (string.Empty, viewName);
                }

                // not checking siteName: return whatever we parsed
                return (parsedSite, parsedFile);
            }

            return (string.Empty, viewName);
        }
    }
}
