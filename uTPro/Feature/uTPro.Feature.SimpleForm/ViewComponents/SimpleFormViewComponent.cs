using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using uTPro.Feature.SimpleForm.Services;

namespace uTPro.Feature.SimpleForm.ViewComponents;

public class SimpleFormViewComponent(ISimpleFormService formService, IWebHostEnvironment env) : ViewComponent
{
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
        if (!string.IsNullOrEmpty(template))
        {
            var path = $"~/Views/Partials/SimpleForm/{template}.cshtml";
            if (FileExists(path)) return path;
        }

        var aliasPath = $"~/Views/Partials/SimpleForm/{alias}.cshtml";
        if (FileExists(aliasPath)) return aliasPath;

        return "~/Views/Partials/SimpleForm/Default.cshtml";
    }

    private bool FileExists(string viewPath)
    {
        var physicalPath = Path.Combine(
            env.ContentRootPath,
            viewPath.Replace("~/", "").Replace("/", Path.DirectorySeparatorChar.ToString()));
        return System.IO.File.Exists(physicalPath);
    }
}
