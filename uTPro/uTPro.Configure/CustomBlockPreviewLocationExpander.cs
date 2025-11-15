using Microsoft.AspNetCore.Mvc.Razor;
using uTPro.Common.Constants;

namespace uTPro.Configure
{
    public class CustomBlockPreviewLocationExpander : IViewLocationExpander
    {
        public void PopulateValues(ViewLocationExpanderContext context)
        {
        }

        public IEnumerable<string> ExpandViewLocations(
            ViewLocationExpanderContext context,
            IEnumerable<string> viewLocations)
        {
            var viewName = context.ViewName;

            yield return CustomPathViews.GetPathViewBlockPreview(viewName);// $"Views/{folder}/blockgrid/Components/{file}.cshtml";
            foreach (var location in viewLocations)
            {
                yield return location;
            }
        }
    }

    public static class CustomPathViews
    {
        public static string GetPathViewBlockPreview(string viewName, string siteName = "")
        {
            (siteName, var fileName) = CustomPathViews.GetSiteAndFileName(viewName, siteName);
            if (!string.IsNullOrEmpty(siteName))
            {
                return $"~/Views/{siteName}/blockgrid/Components/{fileName}.cshtml";
            }
            return viewName;
        }

        public static (string, string) GetSiteAndFileName(string viewName, string siteName = "")
        {
            if (string.IsNullOrEmpty(siteName))
            {
                return (string.Empty, viewName);
            }

            if (viewName.IndexOf($"{siteName}{Prefix.PrefixData}") == 0)
            {
                var parts = viewName.Split(Prefix.PrefixData);
                if (parts.Length >= 2)
                {
                    return (siteName, parts[1]);
                }
            }

            if (viewName.Equals("globalLayout", StringComparison.OrdinalIgnoreCase))
            {
                return (siteName, "_Layout");
            }
            return (string.Empty, viewName);
        }
    }
}
