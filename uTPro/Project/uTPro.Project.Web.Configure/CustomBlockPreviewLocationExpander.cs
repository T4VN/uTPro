using Microsoft.AspNetCore.Mvc.Razor;

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
}
