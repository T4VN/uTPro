using NPoco;
using uTPro.Feature.VideoAnalyzer.Models;

namespace uTPro.Feature.VideoAnalyzer.Persistence;

/// <summary>
/// One analysis attempt for a single platform video. A video may have several rows over
/// time (re-analyses keep history); "the" cached result is the newest Success row per
/// (Platform, VideoId). All datetimes are UTC.
/// </summary>
[TableName("vaVideoAnalysis")]
[PrimaryKey("Id", AutoIncrement = true)]
[ExplicitColumns]
public sealed class VaVideoAnalysis
{
    [Column("Id")]
    public int Id { get; set; }

    /// <summary>Public identifier used in API routes (never expose the auto-increment Id).</summary>
    [Column("Key")]
    public Guid Key { get; set; }

    [Column("Platform")]
    public string Platform { get; set; } = "youtube";

    [Column("VideoId")]
    public string VideoId { get; set; } = string.Empty;

    /// <summary>The URL the initiating member pasted (kept for display; cache lookup uses Platform+VideoId).</summary>
    [Column("SourceUrl")]
    public string SourceUrl { get; set; } = string.Empty;

    [Column("Status")]
    public int Status { get; set; } = (int)AnalysisStatus.Pending;

    [Column("Progress")]
    public int Progress { get; set; }

    [Column("Stage")]
    public string? Stage { get; set; }

    [Column("Title")]
    public string? Title { get; set; }

    [Column("ChannelTitle")]
    public string? ChannelTitle { get; set; }

    [Column("ThumbnailUrl")]
    public string? ThumbnailUrl { get; set; }

    [Column("DurationSeconds")]
    public int DurationSeconds { get; set; }

    /// <summary>YouTube Data API payload (title/description/tags/statistics) as JSON.</summary>
    [Column("MetadataJson")]
    public string? MetadataJson { get; set; }

    /// <summary>Captured transcript segments as JSON: [{"s": 0.0, "t": "..."}].</summary>
    [Column("TranscriptJson")]
    public string? TranscriptJson { get; set; }

    /// <summary>The AI report (see <see cref="VideoReport"/>) as JSON.</summary>
    [Column("ReportJson")]
    public string? ReportJson { get; set; }

    [Column("ReportLanguage")]
    public string? ReportLanguage { get; set; }

    [Column("InitiatedByMemberId")]
    public int? InitiatedByMemberId { get; set; }

    [Column("InitiatedByMemberName")]
    public string? InitiatedByMemberName { get; set; }

    /// <summary>1-based counter per (Platform, VideoId); bumped by "analyze again".</summary>
    [Column("AnalysisVersion")]
    public int AnalysisVersion { get; set; } = 1;

    /// <summary>How many times other requests were served from this row.</summary>
    [Column("CacheHits")]
    public int CacheHits { get; set; }

    /// <summary>Refreshed periodically while the pipeline runs; used to recover stale jobs.</summary>
    [Column("HeartbeatAt")]
    public DateTime? HeartbeatAt { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [Column("CompletedAt")]
    public DateTime? CompletedAt { get; set; }

    [Column("ErrorMessage")]
    public string? ErrorMessage { get; set; }
}
