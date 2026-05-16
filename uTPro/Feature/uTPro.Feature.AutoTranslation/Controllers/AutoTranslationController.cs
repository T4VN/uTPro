using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using uTPro.Feature.AutoTranslation.Models;
using uTPro.Feature.AutoTranslation.Services;

namespace uTPro.Feature.AutoTranslation.Controllers;

/// <summary>
/// Backoffice API for auto-translation.
/// Uses cookie-based backoffice authentication (not Bearer token).
/// Route: /umbraco/api/utpro/auto-translation/...
/// </summary>
[ApiController]
[Route("umbraco/api/utpro/auto-translation")]
[AllowAnonymous]
public class AutoTranslationController : ControllerBase
{
    private readonly IAutoTranslationService _service;

    public AutoTranslationController(IAutoTranslationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Translate values supplied by the client.
    /// </summary>
    [HttpPost("values")]
    public async Task<IActionResult> TranslateValues([FromBody] TranslateRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.TargetCulture))
        {
            return BadRequest("Target culture is required.");
        }

        var result = await _service.TranslateValuesAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Translate the persisted default-language values of a content item and save to target culture.
    /// </summary>
    [HttpGet("content/{key:guid}")]
    public async Task<IActionResult> TranslateContent(
        Guid key,
        [FromQuery] string targetCulture,
        [FromQuery] string? sourceCulture,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetCulture))
        {
            return BadRequest("Target culture is required.");
        }

        var result = await _service.TranslateAndSaveContentAsync(key, targetCulture, sourceCulture, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Translate the persisted default-language values of a media item.
    /// </summary>
    [HttpGet("media/{key:guid}")]
    public async Task<IActionResult> TranslateMedia(
        Guid key,
        [FromQuery] string targetCulture,
        [FromQuery] string? sourceCulture,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetCulture))
        {
            return BadRequest("Target culture is required.");
        }

        var result = await _service.TranslateMediaAsync(key, targetCulture, sourceCulture, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Translate a single piece of text.
    /// </summary>
    [HttpPost("text")]
    public async Task<IActionResult> TranslateText([FromBody] TranslateTextRequest request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.TargetCulture))
        {
            return BadRequest("Target culture is required.");
        }

        var translated = await _service.TranslateTextAsync(
            request.Text ?? string.Empty,
            request.TargetCulture,
            request.SourceCulture,
            request.IsHtml,
            cancellationToken);

        return Ok(new TranslateTextResponse
        {
            SourceCulture = request.SourceCulture ?? string.Empty,
            TargetCulture = request.TargetCulture,
            Text = translated
        });
    }
}

public class TranslateTextRequest
{
    public string? Text { get; set; }
    public string? SourceCulture { get; set; }
    public string TargetCulture { get; set; } = string.Empty;
    public bool IsHtml { get; set; }
}

public class TranslateTextResponse
{
    public string SourceCulture { get; set; } = string.Empty;
    public string TargetCulture { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
