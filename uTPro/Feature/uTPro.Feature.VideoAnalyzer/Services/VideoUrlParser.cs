using System.Text.RegularExpressions;

namespace uTPro.Feature.VideoAnalyzer.Services;

/// <summary>Result of parsing a user-supplied video URL.</summary>
public sealed class VideoUrlParseResult
{
    public bool Success { get; init; }

    /// <summary>Failed when parsing, or when the platform is recognized but not supported yet.</summary>
    public string? Error { get; init; }

    public string Platform { get; init; } = "youtube";

    public string VideoId { get; init; } = string.Empty;

    /// <summary>True when the pasted URL pointed at a playlist; the first/current video is used.</summary>
    public bool IsPlaylist { get; init; }

    private static readonly Regex VideoIdPattern = new("^[A-Za-z0-9_-]{11}$", RegexOptions.Compiled);

    /// <summary>Parses YouTube URLs (watch, youtu.be, shorts, embed, live), bare video IDs,
    /// and recognizes TikTok links as known-but-unsupported.</summary>
    public static VideoUrlParseResult Parse(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return Fail("Please paste a video URL.");
        }

        input = input.Trim();
        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            // Convenience: accept a bare 11-char video ID.
            return VideoIdPattern.IsMatch(input)
                ? Ok("youtube", input, isPlaylist: false)
                : Fail("The value is neither a valid URL nor a video ID.");
        }

        var host = uri.Host.Replace("www.", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("m.", string.Empty, StringComparison.OrdinalIgnoreCase);

        if (host.Equals("youtube.com", StringComparison.OrdinalIgnoreCase)
            || host.Equals("youtube-nocookie.com", StringComparison.OrdinalIgnoreCase))
        {
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2
                && segments[0] is "shorts" or "embed" or "live" or "v"
                && VideoIdPattern.IsMatch(segments[1]))
            {
                return Ok("youtube", segments[1], isPlaylist: QueryContainsPlaylist(uri));
            }

            // /playlist?list=... has no video component.
            if (segments.Length == 1 && segments[0] == "playlist")
            {
                return Fail("Playlist links are not supported yet — paste a single video URL.");
            }

            var videoId = QueryGet(uri, "v");
            if (videoId is not null && VideoIdPattern.IsMatch(videoId))
            {
                return Ok("youtube", videoId, isPlaylist: QueryContainsPlaylist(uri));
            }

            return Fail("Could not find a YouTube video ID in the link.");
        }

        if (host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
        {
            var segment = uri.AbsolutePath.Trim('/');
            return VideoIdPattern.IsMatch(segment)
                ? Ok("youtube", segment, isPlaylist: QueryContainsPlaylist(uri))
                : Fail("Could not find a YouTube video ID in the link.");
        }

        if (host.Equals("tiktok.com", StringComparison.OrdinalIgnoreCase))
        {
            return new VideoUrlParseResult
            {
                Success = false,
                Error = "TikTok support is coming in a later release — for now, paste a YouTube video or Shorts link.",
                Platform = "tiktok",
            };
        }

        return Fail("Only YouTube video links are supported at the moment.");
    }

    private static VideoUrlParseResult Ok(string platform, string videoId, bool isPlaylist)
        => new() { Success = true, Platform = platform, VideoId = videoId, IsPlaylist = isPlaylist };

    private static VideoUrlParseResult Fail(string error)
        => new() { Success = false, Error = error };

    private static string? QueryGet(Uri uri, string key)
        => System.Web.HttpUtility.ParseQueryString(uri.Query)[key];

    private static bool QueryContainsPlaylist(Uri uri)
        => QueryGet(uri, "list") is { Length: > 0 };
}
