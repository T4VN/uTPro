using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using uTPro.Feature.SimpleForm.Models;
using uTPro.Feature.SimpleForm.Services;

namespace uTPro.Feature.SimpleForm.Controllers;

[VersionedApiBackOfficeRoute("utpro/simple-form")]
[ApiExplorerSettings(GroupName = "uTPro Simple Form")]
public class SimpleFormApiController(ISimpleFormService formService) : ManagementApiControllerBase
{
    [HttpPost("list")]
    public IActionResult List() => Ok(formService.GetAllForms());

    [HttpPost("get")]
    public IActionResult Get([FromBody] GetFormRequest request)
    {
        var form = formService.GetForm(request.Id);
        return form != null ? Ok(form) : NotFound(new { message = "Form not found" });
    }

    [HttpPost("save")]
    public IActionResult Save([FromBody] SaveFormRequest request)
    {
        var (success, message, id) = formService.SaveForm(request);
        return success ? Ok(new { message, id }) : BadRequest(new { message });
    }

    [HttpPost("delete")]
    public IActionResult Delete([FromBody] DeleteFormRequest request)
    {
        var (success, message) = formService.DeleteForm(request.Id);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("submissions")]
    public IActionResult Submissions([FromBody] SubmissionListRequest request)
        => Ok(formService.GetSubmissions(request.FormId, request.Skip, request.Take));

    [HttpPost("delete-submission")]
    public IActionResult DeleteSubmission([FromBody] DeleteFormRequest request)
    {
        var (success, message) = formService.DeleteSubmission(request.Id);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("field-types")]
    public IActionResult FieldTypes() => Ok(new[]
    {
        new { type = "text", label = "Text Input" },
        new { type = "email", label = "Email" },
        new { type = "tel", label = "Phone" },
        new { type = "number", label = "Number" },
        new { type = "textarea", label = "Text Area" },
        new { type = "select", label = "Dropdown" },
        new { type = "checkbox", label = "Checkbox" },
        new { type = "radio", label = "Radio Buttons" },
        new { type = "file", label = "File Upload" },
        new { type = "hidden", label = "Hidden Field" },
        new { type = "date", label = "Date Picker" },
        new { type = "url", label = "URL" },
        new { type = "password", label = "Password" },
    });
}
