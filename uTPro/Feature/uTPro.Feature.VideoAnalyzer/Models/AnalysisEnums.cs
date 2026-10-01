namespace uTPro.Feature.VideoAnalyzer.Models;

/// <summary>Lifecycle of an analysis row. Stored as int in the database.</summary>
public enum AnalysisStatus
{
    Pending = 0,
    Processing = 1,
    Success = 2,
    Failed = 3,
}

/// <summary>Supported video platforms. TikTok and friends are planned but not wired yet.</summary>
public enum VideoPlatform
{
    YouTube = 0,
    TikTok = 1,
}
