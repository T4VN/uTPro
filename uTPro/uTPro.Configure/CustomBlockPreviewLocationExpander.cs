using Microsoft.AspNetCore.Mvc.Razor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (viewName.Contains("__"))
            {
                var parts = viewName.Split("__");
                if (parts.Length >= 2)
                {
                    var siteNameOf = parts[0];
                    var fileNameOf = parts[1];
                    return $"~/Views/{(!string.IsNullOrEmpty(siteName) ? siteName : siteNameOf)}/blockgrid/Components/{fileNameOf}.cshtml";
                }
            }
            return viewName;
        }
    }
}
