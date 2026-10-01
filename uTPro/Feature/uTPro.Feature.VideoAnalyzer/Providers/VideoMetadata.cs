namespace uTPro.Feature.VideoAnalyzer.Providers;

/// <summary>Video metadata resolved from the platform's official API (YouTube Data API v3).</summary>
public sealed class VideoMetadata
{
    public string Id { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string ChannelTitle { get; init; } = string.Empty;

    public string ThumbnailUrl { get; init; } = string.Empty;

    public List<string> Tags { get; init; } = [];

    public int DurationSeconds { get; init; }

    public long ViewCount { get; init; }

    public DateTime PublishedAt { get; init; }

    public string PublishedAtText { get; init; } = string.Empty;
}
