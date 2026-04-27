using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using uTPro.Feature.SimpleForm.Services;

namespace uTPro.Feature.SimpleForm.ViewComponents;

public class SimpleFormViewComponent(ISimpleFormService formService, IWebHostEnvironment env) : ViewComponent
{
    /// <summary>
    /// Renders a SimpleForm by alias.
    /// Template resolution order:
    ///   1. Explicit template parameter: ~/Views/Partials/SimpleForm/{template}.cshtml
    ///   2. Form-specific template:      ~/Views/Partials/SimpleForm/{alias}.cshtml
    ///   3. Default template:            ~/Views/Partials/SimpleForm/Default.cshtml
    /// </summary>
    public IViewComponentResult Invoke(
        string alias,
        string? template = null,
        string? cssClass = null,
        string? submitBtnText = null,
        bool? showReset = null,
        string? resetBtnText = null)
    {
        var form = formService.GetFormByAlias(alias);
        if (form == null || !form.IsEnabled)
            return Content($"<!-- SimpleForm '{alias}' not found or disabled -->");

        ViewBag.FormCssClass = cssClass ?? "";
        ViewBag.SubmitBtnText = submitBtnText ?? "Submit";
        ViewBag.ShowReset = showReset;
        ViewBag.ResetBtnText = resetBtnText;

        var viewPath = ResolveTemplate(template, alias);
        return View(viewPath, form);
    }

    private string ResolveTemplate(string? template, string alias)
    {
        // 1. Explicit template
        if (!string.IsNullOrEmpty(template))
        {
            var explicitPath = $"~/Views/Partials/SimpleForm/{template}.cshtml";
            if (ViewExists(explicitPath)) return explicitPath;
        }

        // 2. Form-specific template (by alias)
        var aliasPath = $"~/Views/Partials/SimpleForm/{alias}.cshtml";
        if (ViewExists(aliasPath)) return aliasPath;

        // 3. Default
        return "~/Views/Partials/SimpleForm/Default.cshtml";
    }

    private bool ViewExists(string viewPath)
    {
        var physicalPath = Path.Combine(
            env.ContentRootPath,
            viewPath.Replace("~/", "").Replace("/", Path.DirectorySeparatorChar.ToString()));
        return System.IO.File.Exists(physicalPath);
    }
}
