using Microsoft.Extensions.Options;
using uTPro.Feature.VideoAnalyzer.Configuration;

namespace uTPro.Feature.VideoAnalyzer.Services;

/// <summary>Result of a quota check before initiating a fresh analysis.</summary>
public sealed class QuotaResult
{
    public bool Allowed { get; init; }

    /// <summary>Remaining fresh analyses today (null = unlimited).</summary>
    public int? Remaining { get; init; }

    public string? Message { get; init; }
}

/// <summary>
/// Per-member daily quota. Only FRESH analyses consume quota — cache hits and chat Q&A
/// never do, because they never spend Gemini tokens on a new video pass.
/// </summary>
public interface IQuotaService
{
    Task<QuotaResult> CheckAsync(int memberId, bool isQuotaExempt);
}

internal sealed class QuotaService(IVideoAnalyzerRepository repository, IOptions<VideoAnalyzerOptions> options)
    : IQuotaService
{
    public async Task<QuotaResult> CheckAsync(int memberId, bool isQuotaExempt)
    {
        var limit = options.Value.DailyQuotaPerMember;
        if (limit <= 0 || isQuotaExempt)
        {
            return new QuotaResult { Allowed = true, Remaining = null };
        }

        var utcDayStart = DateTime.UtcNow.Date;
        var used = await repository.CountInitiatedByAsync(memberId, utcDayStart);
        var remaining = Math.Max(0, limit - used);
        return new QuotaResult
        {
            Allowed = remaining > 0,
            Remaining = remaining,
            Message = remaining > 0
                ? null
                : $"You have used all {limit} fresh analyses for today. Cached videos and chat remain unlimited.",
        };
    }
}
