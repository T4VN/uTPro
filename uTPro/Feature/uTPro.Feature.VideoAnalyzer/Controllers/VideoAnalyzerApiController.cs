using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Security;
using uTPro.Feature.VideoAnalyzer.Configuration;
using uTPro.Feature.VideoAnalyzer.Models;
using uTPro.Feature.VideoAnalyzer.Persistence;
using uTPro.Feature.VideoAnalyzer.Providers;
using uTPro.Feature.VideoAnalyzer.Services;

namespace uTPro.Feature.VideoAnalyzer.Controllers;

/// <summary>
/// Public API for the Video Analyzer page. Every endpoint requires a signed-in member in
/// the configured group(s). Fresh analyses consume the per-member daily quota; cache hits
/// and chat never do. POST endpoints rely on the SameSite=Lax member cookie for CSRF.
/// </summary>
[ApiController]
[Route("api/video-analyzer")]
public sealed class VideoAnalyzerApiController(
    IVideoAnalyzerRepository repository,
    IQuotaService quotaService,
    IAnalysisPipeline pipeline,
    IGeminiAnalyzer gemini,
    IMemberManager memberManager,
    IOptions<VideoAnalyzerOptions> options,
    ILogger<VideoAnalyzerApiController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest request, CancellationToken cancellationToken)
    {
        var (member, forbidden) = await RequireMemberAsync();
        if (member is null)
        {
            return forbidden ? Forbid() : Unauthorized();
        }

        var parsed = VideoUrlParseResult.Parse(request.Url);
        if (!parsed.Success)
        {
            return BadRequest(new { error = parsed.Error });
        }

        await pipeline.RecoverStaleJobsAsync();

        // 1. Global cache — a successful analysis by ANY member serves everyone.
        var cached = await repository.GetLatestSuccessAsync(parsed.Platform, parsed.VideoId);
        if (cached is not null && !IsExpired(cached))
        {
            _ = repository.IncrementCacheHitsAsync(cached.Id);
            var quotaAfterHit = await quotaService.CheckAsync(
                GetMemberId(member) ?? 0, await IsQuotaExemptAsync());
            return Ok(new AnalyzeResponse
            {
                CacheHit = true,
                AnalysisKey = cached.Key,
                Report = ParseReport(cached.ReportJson),
                QuotaRemaining = quotaAfterHit.Remaining,
            });
        }

        // 2. Already running? Join the existing job instead of starting a duplicate.
        var active = await repository.GetActiveAsync(parsed.Platform, parsed.VideoId);
        if (active is not null)
        {
            return Ok(new AnalyzeResponse
            {
                CacheHit = false,
                AnalysisKey = active.Key,
                Message = "This video is already being analyzed.",
            });
        }

        // 3. Quota gate — only fresh analyses spend tokens.
        var isAdmin = await IsQuotaExemptAsync();
        var quota = await quotaService.CheckAsync(GetMemberId(member) ?? 0, isAdmin);
        if (!quota.Allowed)
        {
            return StatusCode(429, new { error = quota.Message });
        }

        // 4. Create the job row and kick off the background pipeline.
        var language = NormalizeLanguage(request.Language) ?? options.Value.DefaultReportLanguage;
        var entity = new VaVideoAnalysis
        {
            Key = Guid.NewGuid(),
            Platform = parsed.Platform,
            VideoId = parsed.VideoId,
            SourceUrl = request.Url.Trim(),
            Status = (int)AnalysisStatus.Pending,
            Stage = "queued",
            ReportLanguage = language,
            InitiatedByMemberId = GetMemberId(member),
            InitiatedByMemberName = member.UserName,
            AnalysisVersion = await repository.GetMaxVersionAsync(parsed.Platform, parsed.VideoId) + 1,
            CreatedAt = DateTime.UtcNow,
            HeartbeatAt = DateTime.UtcNow,
        };
        await repository.InsertAsync(entity);
        await pipeline.StartAsync(entity);

        return Ok(new AnalyzeResponse
        {
            CacheHit = false,
            AnalysisKey = entity.Key,
            QuotaRemaining = quota.Remaining,
            Message = parsed.IsPlaylist ? "The link contained a playlist — analyzing the current video only." : null,
        });
    }

    [HttpGet("jobs/{key:guid}")]
    public async Task<IActionResult> JobStatus(Guid key, CancellationToken cancellationToken)
    {
        var (member, forbidden) = await RequireMemberAsync();
        if (member is null)
        {
            return forbidden ? Forbid() : Unauthorized();
        }

        await pipeline.RecoverStaleJobsAsync();
        var entity = await repository.GetByKeyAsync(key);
        if (entity is null)
        {
            return NotFound();
        }

        return Ok(new JobStatusResponse
        {
            Key = entity.Key,
            Status = (AnalysisStatus)entity.Status,
            Progress = entity.Progress,
            Stage = entity.Stage ?? string.Empty,
            Error = entity.ErrorMessage,
        });
    }

    [HttpGet("reports/{key:guid}")]
    public async Task<IActionResult> Report(Guid key, CancellationToken cancellationToken)
    {
        var (member, forbidden) = await RequireMemberAsync();
        if (member is null)
        {
            return forbidden ? Forbid() : Unauthorized();
        }

        var entity = await repository.GetByKeyAsync(key);
        if (entity is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            key = entity.Key,
            status = (AnalysisStatus)entity.Status,
            title = entity.Title,
            channelTitle = entity.ChannelTitle,
            thumbnailUrl = entity.ThumbnailUrl,
            durationSeconds = entity.DurationSeconds,
            sourceUrl = entity.SourceUrl,
            reportLanguage = entity.ReportLanguage,
            createdAt = entity.CreatedAt,
            completedAt = entity.CompletedAt,
            analysisVersion = entity.AnalysisVersion,
            cacheHits = entity.CacheHits,
            error = entity.ErrorMessage,
            report = ParseReport(entity.ReportJson),
        });
    }

    [HttpPost("reports/{key:guid}/chat")]
    public async Task<IActionResult> Chat(Guid key, [FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        var (member, forbidden) = await RequireMemberAsync();
        if (member is null)
        {
            return forbidden ? Forbid() : Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Empty message." });
        }

        var entity = await repository.GetByKeyAsync(key);
        if (entity is null)
        {
            return NotFound();
        }

        if (entity.Status != (int)AnalysisStatus.Success)
        {
            return Conflict(new { error = "The analysis is not finished yet." });
        }

        var totalMessages = await repository.CountChatAsync(entity.Id);
        if (totalMessages >= options.Value.MaxChatMessagesPerAnalysis * 2)
        {
            return StatusCode(429, new { error = "This analysis has reached its chat message limit." });
        }

        var history = (await repository.GetChatAsync(entity.Id))
            .TakeLast(12)
            .Select(m => (m.Role, m.Content))
            .ToList();

        string reply;
        try
        {
            reply = await gemini.ChatAsync(
                entity.ReportJson ?? "{}",
                entity.TranscriptJson,
                history,
                request.Message.Trim(),
                cancellationToken);
        }
        catch (GeminiQuotaException)
        {
            logger.LogWarning("VideoAnalyzer: chat blocked by quota for {Key}.", key);
            return StatusCode(429, new { error = "The AI quota is exhausted — please try again later." });
        }

        var now = DateTime.UtcNow;
        await repository.AddChatMessageAsync(new VaChatMessage
        {
            AnalysisId = entity.Id,
            MemberId = GetMemberId(member) ?? 0,
            MemberName = member.UserName,
            Role = "user",
            Content = request.Message.Trim(),
            CreatedAt = now,
        });
        await repository.AddChatMessageAsync(new VaChatMessage
        {
            AnalysisId = entity.Id,
            MemberId = GetMemberId(member) ?? 0,
            MemberName = "assistant",
            Role = "assistant",
            Content = reply,
            CreatedAt = now,
        });

        return Ok(new ChatResponse
        {
            Reply = reply,
            RemainingMessages = Math.Max(0, options.Value.MaxChatMessagesPerAnalysis * 2 - totalMessages - 2),
        });
    }

    /// <summary>Forces a fresh analysis of an existing report's video (consumes quota).</summary>
    [HttpPost("reports/{key:guid}/refresh")]
    public async Task<IActionResult> Refresh(Guid key, CancellationToken cancellationToken)
    {
        var (member, forbidden) = await RequireMemberAsync();
        if (member is null)
        {
            return forbidden ? Forbid() : Unauthorized();
        }

        var entity = await repository.GetByKeyAsync(key);
        if (entity is null)
        {
            return NotFound();
        }

        await pipeline.RecoverStaleJobsAsync();

        var active = await repository.GetActiveAsync(entity.Platform, entity.VideoId);
        if (active is not null)
        {
            return Ok(new AnalyzeResponse
            {
                CacheHit = false,
                AnalysisKey = active.Key,
                Message = "This video is already being analyzed.",
            });
        }

        var isAdmin = await IsQuotaExemptAsync();
        var quota = await quotaService.CheckAsync(GetMemberId(member) ?? 0, isAdmin);
        if (!quota.Allowed)
        {
            return StatusCode(429, new { error = quota.Message });
        }

        var fresh = new VaVideoAnalysis
        {
            Key = Guid.NewGuid(),
            Platform = entity.Platform,
            VideoId = entity.VideoId,
            SourceUrl = entity.SourceUrl,
            Status = (int)AnalysisStatus.Pending,
            Stage = "queued",
            ReportLanguage = entity.ReportLanguage ?? options.Value.DefaultReportLanguage,
            InitiatedByMemberId = GetMemberId(member),
            InitiatedByMemberName = member.UserName,
            AnalysisVersion = await repository.GetMaxVersionAsync(entity.Platform, entity.VideoId) + 1,
            CreatedAt = DateTime.UtcNow,
            HeartbeatAt = DateTime.UtcNow,
        };
        await repository.InsertAsync(fresh);
        await pipeline.StartAsync(fresh);

        return Ok(new AnalyzeResponse
        {
            CacheHit = false,
            AnalysisKey = fresh.Key,
            QuotaRemaining = quota.Remaining,
        });
    }

    // --- auth helpers ---------------------------------------------------------------

    /// <summary>Returns the current member, or (null, forbidden) when signed in without the right group.</summary>
    private async Task<(MemberIdentityUser? Member, bool Forbidden)> RequireMemberAsync()
    {
        var member = await memberManager.GetCurrentMemberAsync();
        if (member is null)
        {
            return (null, false);
        }

        var allowed = await memberManager.IsMemberAuthorizedAsync(
            allowGroups: new[] { options.Value.MemberUserGroup, options.Value.MemberAdminGroup });
        return (allowed ? member : null, !allowed);
    }

    private async Task<bool> IsQuotaExemptAsync()
        => await memberManager.IsMemberAuthorizedAsync(allowGroups: new[] { options.Value.MemberAdminGroup });

    private static int? GetMemberId(MemberIdentityUser member)
        => int.TryParse(member.Id, out var id) ? id : null;

    // --- helpers --------------------------------------------------------------------

    private bool IsExpired(VaVideoAnalysis cached)
    {
        var ttlDays = options.Value.CacheTtlDays;
        return ttlDays > 0 && cached.CompletedAt.HasValue
            && cached.CompletedAt.Value < DateTime.UtcNow.AddDays(-ttlDays);
    }

    private static VideoReport? ParseReport(string? json)
        => string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<VideoReport>(json, JsonOptions);

    private static string? NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var code = language.Trim().ToLowerInvariant();
        return code.Length is 2 or 5 ? code : null;
    }
}
