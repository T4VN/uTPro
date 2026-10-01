using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using uTPro.Feature.VideoAnalyzer.Configuration;

namespace uTPro.Feature.VideoAnalyzer.Providers;

/// <summary>Official metadata source for YouTube videos (YouTube Data API v3, free quota).</summary>
public interface IYouTubeMetadataProvider
{
    /// <summary>Fetches metadata for one video, or null when the API key is missing or the video is not found.</summary>
    Task<VideoMetadata?> GetVideoAsync(string videoId);
}

internal sealed class YouTubeDataApiProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<VideoAnalyzerOptions> options,
    ILogger<YouTubeDataApiProvider> logger) : IYouTubeMetadataProvider
{
    private const string HttpClientName = "uTProVideoAnalyzerYouTubeData";
    private const string ApiBase = "https://www.googleapis.com/youtube/v3/videos";

    public async Task<VideoMetadata?> GetVideoAsync(string videoId)
    {
        var apiKey = options.Value.YouTubeDataApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("VideoAnalyzer: YouTubeDataApiKey is not configured — skipping official metadata.");
            return null;
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        var url = $"{ApiBase}?part=snippet,contentDetails,statistics&id={Uri.EscapeDataString(videoId)}&key={Uri.EscapeDataString(apiKey)}";

        using var response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("VideoAnalyzer: YouTube Data API returned {StatusCode} for video {VideoId}.",
                (int)response.StatusCode, videoId);
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        var items = payload.GetPropertyOrNull("items");
        if (items is null || items.Value.GetArrayLength() == 0)
        {
            return null;
        }

        var item = items.Value[0];
        var snippet = item.GetPropertyOrNull("snippet");
        var statistics = item.GetPropertyOrNull("statistics");
        var thumbnails = snippet?.GetPropertyOrNull("thumbnails");

        return new VideoMetadata
        {
            Id = item.GetPropertyOrNull("id")?.GetString() ?? videoId,
            Title = snippet?.GetPropertyOrNull("title")?.GetString() ?? string.Empty,
            Description = snippet?.GetPropertyOrNull("description")?.GetString() ?? string.Empty,
            ChannelTitle = snippet?.GetPropertyOrNull("channelTitle")?.GetString() ?? string.Empty,
            ThumbnailUrl =
                thumbnails?.GetPropertyOrNull("maxres")?.GetPropertyOrNull("url")?.GetString()
                ?? thumbnails?.GetPropertyOrNull("high")?.GetPropertyOrNull("url")?.GetString()
                ?? string.Empty,
            Tags = snippet?.GetPropertyOrNull("tags") is { } tags
                ? tags.EnumerateArray().Select(t => t.GetString() ?? string.Empty).Where(t => t.Length > 0).ToList()
                : [],
            DurationSeconds = ParseIsoDuration(
                item.GetPropertyOrNull("contentDetails")?.GetPropertyOrNull("duration")?.GetString() ?? string.Empty),
            ViewCount = statistics?.GetPropertyOrNull("viewCount")?.GetInt64() ?? 0,
            PublishedAt = DateTime.TryParse(
                snippet?.GetPropertyOrNull("publishedAt")?.GetString(), out var publishedAt)
                ? publishedAt.ToUniversalTime()
                : default,
            PublishedAtText = snippet?.GetPropertyOrNull("publishedAt")?.GetString() ?? string.Empty,
        };
    }

    /// <summary>Parses ISO-8601 durations like PT1H2M10S (YouTube's format) to total seconds.</summary>
    internal static int ParseIsoDuration(string isoDuration)
    {
        if (string.IsNullOrEmpty(isoDuration))
        {
            return 0;
        }

        var span = isoDuration.AsSpan();
        if (span.Length < 3 || span[0] != 'P')
        {
            return 0;
        }

        int hours = 0, minutes = 0, seconds = 0;
        var number = 0;
        var hasNumber = false;

        for (var i = 1; i < span.Length; i++)
        {
            var c = span[i];
            if (char.IsDigit(c))
            {
                number = (number * 10) + (c - '0');
                hasNumber = true;
            }
            else if (hasNumber)
            {
                switch (c)
                {
                    case 'H': hours = number; break;
                    case 'M': minutes = number; break;
                    case 'S': seconds = number; break;
                }

                number = 0;
                hasNumber = false;
            }
        }

        return (hours * 3600) + (minutes * 60) + seconds;
    }
}

/// <summary>Small helpers for reading optional members of System.Text.Json DOM values.</summary>
internal static class JsonElementExtensions
{
    public static JsonElement? GetPropertyOrNull(this JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var value)
            && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
            ? value
            : null;
}
