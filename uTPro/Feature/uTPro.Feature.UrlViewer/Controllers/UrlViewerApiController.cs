using Microsoft.AspNetCore.Mvc;
using uTPro.Feature.UrlViewer.Models;
using uTPro.Feature.UrlViewer.Services;

namespace uTPro.Feature.UrlViewer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UrlViewerApiController : ControllerBase
{
    private readonly IUrlViewerService _urlViewerService;

    public UrlViewerApiController(IUrlViewerService urlViewerService)
    {
        _urlViewerService = urlViewerService;
    }

    /// <summary>
    /// Fetch a URL with custom User-Agent and Referrer settings.
    /// </summary>
    [HttpPost("fetch")]
    public async Task<IActionResult> Fetch([FromBody] UrlViewerRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new { error = "URL is required." });
        }

        var result = await _urlViewerService.FetchUrlAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get available User-Agent presets.
    /// </summary>
    [HttpGet("user-agents")]
    public IActionResult GetUserAgents()
    {
        var agents = UserAgentPresets.Agents.Select(a => new
        {
            key = a.Key,
            value = a.Value
        });
        return Ok(agents);
    }

    /// <summary>
    /// Get available Referrer presets.
    /// </summary>
    [HttpGet("referrers")]
    public IActionResult GetReferrers()
    {
        var referrers = ReferrerPresets.Referrers.Select(r => new
        {
            key = r.Key,
            value = r.Value
        });
        return Ok(referrers);
    }
}
