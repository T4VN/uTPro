namespace uTPro.Feature.VideoAnalyzer.Configuration;

/// <summary>
/// Options for the Video Analyzer feature, bound from the
/// <c>uTPro:Feature:VideoAnalyzer</c> section of appsettings.
///
/// API keys are read from this section but are expected to come from environment
/// variables / user-secrets in production — never commit real keys.
/// </summary>
public sealed class VideoAnalyzerOptions
{
    public const string SectionPath = "uTPro:Feature:VideoAnalyzer";

    /// <summary>YouTube Data API v3 key (Google Cloud console). Required for metadata.</summary>
    public string? YouTubeDataApiKey { get; set; }

    /// <summary>Gemini API key (Google AI Studio). Required for content analysis.</summary>
    public string? GeminiApiKey { get; set; }

    /// <summary>Gemini model used for the primary video-URL analysis pass.</summary>
    public string GeminiModel { get; set; } = "gemini-2.5-flash";

    /// <summary>Gemini model used for chat follow-ups (cheaper is fine — text only).</summary>
    public string GeminiChatModel { get; set; } = "gemini-2.5-flash-lite";

    /// <summary>Cached successful analyses older than this are re-analyzed on request. 0 = never expire.</summary>
    public int CacheTtlDays { get; set; } = 0;

    /// <summary>Videos longer than this are rejected up-front to protect AI quota.</summary>
    public int MaxVideoMinutes { get; set; } = 60;

    /// <summary>Fresh analyses a member may initiate per calendar day (UTC). Admin-group members are exempt.</summary>
    public int DailyQuotaPerMember { get; set; } = 5;

    /// <summary>Jobs stuck in Processing without a heartbeat for longer than this are marked failed so the video can be retried.</summary>
    public int StaleJobMinutes { get; set; } = 10;

    /// <summary>Maximum chat messages (user + assistant) kept per analysis.</summary>
    public int MaxChatMessagesPerAnalysis { get; set; } = 50;

    /// <summary>Default language for generated reports when the request does not specify one.</summary>
    public string DefaultReportLanguage { get; set; } = "vi";

    /// <summary>Member group alias allowed to use the analyzer.</summary>
    public string MemberUserGroup { get; set; } = "videoAnalyzerUsers";

    /// <summary>Member group alias exempt from the daily quota.</summary>
    public string MemberAdminGroup { get; set; } = "videoAnalyzerAdmins";

    /// <summary>Preferred caption languages for the transcript fallback, in order.</summary>
    public string[] PreferredTranscriptLanguages { get; set; } = ["vi", "en"];
}
