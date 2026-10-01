using System.Text.Json.Serialization;

namespace uTPro.Feature.VideoAnalyzer.Models;

/// <summary>
/// The AI-generated report for one video. Serialized as a single JSON blob in the
/// <c>vaVideoAnalysis.ReportJson</c> column — keep property names stable, older rows
/// are never re-written when this shape evolves.
/// </summary>
public sealed class VideoReport
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("topics")]
    public List<string> Topics { get; set; } = [];

    [JsonPropertyName("mentions")]
    public List<VideoMention> Mentions { get; set; } = [];

    [JsonPropertyName("sentiment")]
    public string Sentiment { get; set; } = string.Empty;

    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; } = [];

    [JsonPropertyName("timeline")]
    public List<VideoChapter> Timeline { get; set; } = [];

    [JsonPropertyName("ageRating")]
    public AgeRating AgeRating { get; set; } = new();

    /// <summary>Timestamped digest segments used as chat Q&A context when no transcript exists.</summary>
    [JsonPropertyName("digest")]
    public List<VideoDigestSegment> Digest { get; set; } = [];

    /// <summary>Where the content evidence came from: "ai-video" (Gemini watched the video) or "transcript".</summary>
    [JsonPropertyName("contentSource")]
    public string ContentSource { get; set; } = string.Empty;

    [JsonPropertyName("modelName")]
    public string ModelName { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;
}

/// <summary>A product / person / place / organization named in the video.</summary>
public sealed class VideoMention
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "other";

    [JsonPropertyName("context")]
    public string Context { get; set; } = string.Empty;
}

/// <summary>One chapter on the video timeline. Seconds are absolute from video start.</summary>
public sealed class VideoChapter
{
    [JsonPropertyName("startSeconds")]
    public double StartSeconds { get; set; }

    [JsonPropertyName("endSeconds")]
    public double EndSeconds { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>One digest segment: what is being said around <see cref="StartSeconds"/>.</summary>
public sealed class VideoDigestSegment
{
    [JsonPropertyName("startSeconds")]
    public double StartSeconds { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Age suitability following the Vietnamese film rating scale (Decree 81/2023/ND-CP):
/// P, K, C13, C16, C18. Advisory only — always shown with an AI disclaimer in the UI.
/// </summary>
public sealed class AgeRating
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("flags")]
    public List<string> Flags { get; set; } = [];

    [JsonPropertyName("reasons")]
    public List<string> Reasons { get; set; } = [];

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}
