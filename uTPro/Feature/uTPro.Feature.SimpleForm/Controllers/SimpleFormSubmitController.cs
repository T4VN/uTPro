using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using uTPro.Feature.SimpleForm.Models;
using uTPro.Feature.SimpleForm.Services;

namespace uTPro.Feature.SimpleForm.Controllers;

[ApiController]
[Route("api/utpro/simple-form")]
public class SimpleFormSubmitController(ISimpleFormService formService) : ControllerBase
{
    [HttpPost("submit")]
    public IActionResult Submit([FromBody] SubmitFormRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = HttpContext.Request.Headers.UserAgent.ToString();
        var (success, message) = formService.SubmitForm(request.Alias, request.Data, ip, ua);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpGet("render/{alias}")]
    public IActionResult RenderForm(string alias)
    {
        var form = formService.GetFormByAlias(alias);
        if (form == null || !form.IsEnabled) return NotFound(new { message = "Form not found" });
        return Ok(form);
    }
}
