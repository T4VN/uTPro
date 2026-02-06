using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Cache.PropertyEditors;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Community.BlockPreview;
using Umbraco.Community.BlockPreview.Interfaces;
using Umbraco.Community.BlockPreview.Services;
using uTPro.Common.Constants;
using static System.Net.Mime.MediaTypeNames;

namespace uTPro.Project.Web.Configure
{
    public class CustomBlockPreviewService(
        ITempDataProvider tempDataProvider,
        IViewComponentHelperWrapper viewComponentHelperWrapper,
        IRazorViewEngine razorViewEngine,
        ITypeFinder typeFinder,
        BlockEditorConverter blockEditorConverter,
        IViewComponentSelector viewComponentSelector,
        IPublishedValueFallback publishedValueFallback,
        IOptions<BlockPreviewOptions> options,
        IJsonSerializer jsonSerializer,
        IContentTypeService contentTypeService,
        IDataTypeService dataTypeService,
        AppCaches appCaches,
        IWebHostEnvironment webHostEnvironment,
        IBlockEditorElementTypeCache elementTypeCache,
        ILogger<BlockPreviewService> logger) : BlockPreviewService(tempDataProvider, viewComponentHelperWrapper, razorViewEngine, typeFinder, blockEditorConverter, viewComponentSelector, publishedValueFallback, options, jsonSerializer, contentTypeService, dataTypeService, appCaches, webHostEnvironment, elementTypeCache, logger)
    {
        readonly IRazorViewEngine _razorViewEngine = razorViewEngine;

        protected override ViewEngineResult? GetViewResult(BlockPreviewContext context)
        {
            var blockGrid = CustomPathViews.GetPathViewBlockGridPreview("~/Views/Partials/blockgrid/Components/" + context.ContentAlias, isCheckSiteName: false);
            if (!string.IsNullOrEmpty(blockGrid))
            {
                if (string.IsNullOrEmpty(Path.GetExtension(blockGrid)))
                {
                    return _razorViewEngine.GetView("", viewPath: blockGrid + ".cshtml", isMainPage: false);
                }
                else
                {
                    return _razorViewEngine.GetView("", viewPath: blockGrid, isMainPage: false);
                }
            }
            return base.GetViewResult(context);
        }
    }

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
            var blockGrid = CustomPathViews.GetPathViewBlockGridPreview(viewName, isCheckSiteName: false);
            if (!string.IsNullOrEmpty(blockGrid) && !viewLocations.Contains(blockGrid, StringComparer.OrdinalIgnoreCase))
            {
                yield return blockGrid;
            }

            var blockGridTemplate = CustomPathViews.GetPathViewBlockPreview("Templates", "blockgrid", viewName, isCheckSiteName: false);
            if (!string.IsNullOrEmpty(blockGridTemplate) && !viewLocations.Contains(blockGridTemplate, StringComparer.OrdinalIgnoreCase))
            {
                yield return blockGridTemplate;
            }

            var blockList = CustomPathViews.GetPathViewBlockListPreview(viewName, isCheckSiteName: false);
            if (!string.IsNullOrEmpty(blockList) && !viewLocations.Contains(blockList, StringComparer.OrdinalIgnoreCase))
            {
                yield return blockList;
            }

            var blockListTemplate = CustomPathViews.GetPathViewBlockPreview("Templates", "blocklist", viewName, isCheckSiteName: false);
            if (!string.IsNullOrEmpty(blockListTemplate) && !viewLocations.Contains(blockListTemplate, StringComparer.OrdinalIgnoreCase))
            {
                yield return blockListTemplate;
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

        //public static async Task<IHtmlContent> GetPreviewBlockGridItemsHtmlAsync(this IHtmlHelper<dynamic> html, IEnumerable<BlockGridItem> items, string siteName = "")
        //{
        //    string viewName = string.Empty;
        //    if (string.IsNullOrEmpty(siteName))
        //    {
        //        viewName = $"~/Views/{siteName}/blockgrid/Templates/items.cshtml";
        //    }
        //    return await html.GetBlockGridItemsHtmlAsync(items, viewName);
        //}

        //public static async Task<IHtmlContent> GetPreviewBlockGridItemAreasHtmlAsync(this IHtmlHelper<dynamic> html, BlockGridItem item, string siteName = "")
        //{
        //    string viewName = string.Empty;
        //    if (string.IsNullOrEmpty(siteName))
        //    {
        //        viewName = $"~/Views/{siteName}/blockgrid/Templates/areas.cshtml";
        //    }
        //    return await html.GetBloc(items, viewName);
        //}

        public static string? GetPathViewBlockGridPreview(string viewName, string siteName = "", bool isCheckSiteName = true)
        {
            return GetPathViewBlockPreview("Components", "blockgrid", viewName, siteName, isCheckSiteName);
        }

        public static string? GetPathViewBlockListPreview(string viewName, string siteName = "", bool isCheckSiteName = true)
        {
            return GetPathViewBlockPreview("Components", "blocklist", viewName, siteName, isCheckSiteName);
        }

        public static string? GetPathViewBlockPreview(string type, string blockType, string viewName, string siteName = "", bool isCheckSiteName = true)
        {
            if (string.IsNullOrEmpty(viewName))
                return null;

            var cacheKey = string.Concat(type, blockType, "|", viewName + "|", siteName ?? string.Empty, "|", isCheckSiteName ? "1" : "0");
            if (s_pathCache.TryGetValue(cacheKey, out var cached))
            {
                if (cached != null)
                {
                    return cached;
                }
            }

            string result = viewName;

            var (site, fileName) = GetSiteAndFileName(viewName, siteName ?? string.Empty, isCheckSiteName);
            if (!string.IsNullOrEmpty(site) && !string.IsNullOrEmpty(fileName))
            {
                if (site.Equals(siteName, StringComparison.OrdinalIgnoreCase) || site.Contains(blockType + "/Components", StringComparison.OrdinalIgnoreCase))
                {
                    // Basic sanitization: remove leading slashes and keep only filename part to avoid traversal
                    fileName = fileName.TrimStart('/', '\\');
                    if (fileName.IndexOfAny(['\\', '/']) >= 0)
                    {
                        fileName = Path.GetFileName(fileName);
                    }
                    if (site.Contains(blockType + "/Components", StringComparison.OrdinalIgnoreCase))
                    {
                        site = Path.GetFileName(site);
                    }
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        result = $"~/Views/{site}/{blockType}/{type}/{string.Join("/", fileName.Split(Prefix.PrefixData))}.cshtml";
                    }
                }
                else
                {
                    var siteNameWithFolderTemplate = site.Split("/") ?? [];
                    if (siteNameWithFolderTemplate.Length > 1 && siteNameWithFolderTemplate?.FirstOrDefault()?.Equals(blockType, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        result = $"~/Views/{siteNameWithFolderTemplate[1]}/{blockType}/Templates/{string.Join("/", fileName.Split(Prefix.PrefixData))}.cshtml";
                    }
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
            var idx = viewName.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
            {
                var parsedSite = viewName[..idx];
                var parsedFile = idx + prefix.Length < viewName.Length ? viewName[(idx + prefix.Length)..] : string.Empty;

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
