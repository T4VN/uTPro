using uTPro.Feature.VideoAnalyzer.Persistence;

namespace uTPro.Feature.VideoAnalyzer.Services;

/// <summary>Data access for the analyzer's own tables (cross-DB via Umbraco's scope/database stack).</summary>
public interface IVideoAnalyzerRepository
{
    /// <summary>Newest successful analysis for a video, or null. TTL filtering happens in the caller.</summary>
    Task<VaVideoAnalysis?> GetLatestSuccessAsync(string platform, string videoId);

    /// <summary>Non-terminal row (Pending/Processing) for a video, if any — used to dedupe concurrent submissions.</summary>
    Task<VaVideoAnalysis?> GetActiveAsync(string platform, string videoId);

    Task<VaVideoAnalysis?> GetByKeyAsync(Guid key);

    Task<int> InsertAsync(VaVideoAnalysis entity);

    Task UpdateAsync(VaVideoAnalysis entity);

    /// <summary>Heartbeat-only update so the background worker never races full-row saves.</summary>
    Task TouchHeartbeatAsync(int id, DateTime utcNow);

    Task IncrementCacheHitsAsync(int id);

    /// <summary>Highest AnalysisVersion seen for a video (0 when none) — used to bump re-analyses.</summary>
    Task<int> GetMaxVersionAsync(string platform, string videoId);

    /// <summary>Rows the member initiated today (any status) — the quota counter.</summary>
    Task<int> CountInitiatedByAsync(int memberId, DateTime utcDayStart);

    /// <summary>Processing rows whose heartbeat is older than the cutoff — stale, recoverable.</summary>
    Task<List<VaVideoAnalysis>> GetStaleProcessingAsync(DateTime utcCutoff);

    Task AddChatMessageAsync(VaChatMessage message);

    Task<List<VaChatMessage>> GetChatAsync(int analysisId);

    Task<int> CountChatAsync(int analysisId);
}
