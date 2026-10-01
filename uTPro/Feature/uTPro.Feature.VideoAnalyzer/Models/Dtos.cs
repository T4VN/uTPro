namespace uTPro.Feature.VideoAnalyzer.Models;

/// <summary>Request body for POST /api/video-analyzer/analyze.</summary>
public sealed class AnalyzeRequest
{
    public string Url { get; set; } = string.Empty;

    /// <summary>Optional report language override (e.g. "vi", "en"). Defaults to the configured language.</summary>
    public string? Language { get; set; }
}

/// <summary>Response for POST /api/video-analyzer/analyze — either a cache hit or a new job.</summary>
public sealed class AnalyzeResponse
{
    /// <summary>True when a successful analysis already existed and is returned without touching AI quota.</summary>
    public bool CacheHit { get; set; }

    public Guid AnalysisKey { get; set; }

    /// <summary>Remaining fresh analyses today for the current member (null = unlimited).</summary>
    public int? QuotaRemaining { get; set; }

    public VideoReport? Report { get; set; }

    public string? Message { get; set; }
}

/// <summary>Response for GET /api/video-analyzer/jobs/{key} — progress polling.</summary>
public sealed class JobStatusResponse
{
    public Guid Key { get; set; }

    public AnalysisStatus Status { get; set; }

    /// <summary>0-100 progress hint for the UI progress bar.</summary>
    public int Progress { get; set; }

    /// <summary>Human-readable current stage (localized client-side via stage key).</summary>
    public string Stage { get; set; } = string.Empty;

    public string? Error { get; set; }
}

/// <summary>Request body for POST /api/video-analyzer/reports/{key}/chat.</summary>
public sealed class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

/// <summary>Response for the chat endpoint.</summary>
public sealed class ChatResponse
{
    public string Reply { get; set; } = string.Empty;

    public int RemainingMessages { get; set; }
}
