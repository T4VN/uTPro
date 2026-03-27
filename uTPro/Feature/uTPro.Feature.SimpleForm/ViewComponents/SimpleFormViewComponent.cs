using Microsoft.AspNetCore.Mvc;
using uTPro.Feature.SimpleForm.Services;

namespace uTPro.Feature.SimpleForm.ViewComponents;

public class SimpleFormViewComponent(ISimpleFormService formService) : ViewComponent
{
    public IViewComponentResult Invoke(string alias, string? cssClass = null, string? submitBtnText = null)
    {
        var form = formService.GetFormByAlias(alias);
        if (form == null || !form.IsEnabled)
            return Content($"<!-- SimpleForm '{alias}' not found or disabled -->");

        ViewBag.FormCssClass = cssClass ?? "";
        ViewBag.SubmitBtnText = submitBtnText ?? "Submit";
        return View("~/Views/Partials/SimpleForm/Default.cshtml", form);
    }
}
