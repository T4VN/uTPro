namespace uTPro.Feature.UrlViewer.Models;

public class UrlViewerRequest
{
    /// <summary>
    /// The URL to fetch (without scheme), e.g. "example.com" or "example.com/page"
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// URL scheme: "http" or "https"
    /// </summary>
    public string Scheme { get; set; } = "https";

    /// <summary>
    /// User-Agent preset key. Supported values:
    /// "googlebot-smartphone", "googlebot-desktop", "bingbot", "chrome", "firefox", "edge"
    /// </summary>
    public string UserAgent { get; set; } = "googlebot-smartphone";

    /// <summary>
    /// Referrer preset key. Supported values:
    /// "google", "bing", "yahoo", "none"
    /// </summary>
    public string Referrer { get; set; } = "google";
}

/// <summary>
/// Represents a single HTTP hop in the redirect chain.
/// </summary>
public class RedirectHop
{
    public string Url { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public string RawHeaders { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = [];
}

/// <summary>
/// Analysis results for cloaking/spam detection.
/// </summary>
public class ContentAnalysis
{
    /// <summary>
    /// Meta tags found in the HTML (name/property → content).
    /// </summary>
    public List<MetaTagInfo> MetaTags { get; set; } = [];

    /// <summary>
    /// Words commonly used in spam/hack injections found in the content.
    /// </summary>
    public List<SpamWordMatch> SpamWords { get; set; } = [];

    /// <summary>
    /// CSS rules that hide elements (display:none, visibility:hidden, etc.).
    /// </summary>
    public List<string> HiddenCssRules { get; set; } = [];

    /// <summary>
    /// JavaScript issues found in the HTML source.
    /// </summary>
    public List<JsIssue> JsIssues { get; set; } = [];

    /// <summary>
    /// Cloaking detection result — compares bot vs browser response.
    /// </summary>
    public CloakingResult? Cloaking { get; set; }
}

/// <summary>
/// Result of comparing bot response vs Chrome browser response.
/// </summary>
public class CloakingResult
{
    /// <summary>
    /// True if cloaking is detected (title or content significantly different).
    /// </summary>
    public bool IsCloaked { get; set; }

    /// <summary>
    /// Title seen by the selected bot UA.
    /// </summary>
    public string BotTitle { get; set; } = string.Empty;

    /// <summary>
    /// Title seen by Chrome (normal browser).
    /// </summary>
    public string ChromeTitle { get; set; } = string.Empty;

    /// <summary>
    /// Content length (bytes) seen by the bot.
    /// </summary>
    public long BotContentLength { get; set; }

    /// <summary>
    /// Content length (bytes) seen by Chrome.
    /// </summary>
    public long ChromeContentLength { get; set; }

    /// <summary>
    /// Absolute difference in bytes between bot and Chrome content.
    /// </summary>
    public long ContentDifference { get; set; }

    /// <summary>
    /// True if the title differs between bot and Chrome.
    /// </summary>
    public bool TitleDiffers { get; set; }

    /// <summary>
    /// Human-readable summary messages.
    /// </summary>
    public List<string> Messages { get; set; } = [];
}

public class MetaTagInfo
{
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class SpamWordMatch
{
    public string Word { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// A JavaScript issue detected by static analysis of the HTML source.
/// </summary>
public class JsIssue
{
    /// <summary>
    /// Severity: "error", "warning", "info"
    /// </summary>
    public string Severity { get; set; } = "warning";

    /// <summary>
    /// Category: "syntax", "security", "deprecated", "resource", "suspicious"
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the issue.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The code snippet or pattern that triggered the issue.
    /// </summary>
    public string Snippet { get; set; } = string.Empty;
}

public class UrlViewerResponse
{
    public bool Success { get; set; }
    public string RequestedUrl { get; set; } = string.Empty;
    public string FinalUrl { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public long ContentLength { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string UserAgentUsed { get; set; } = string.Empty;
    public string ReferrerUsed { get; set; } = string.Empty;
    public long ElapsedMilliseconds { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Full redirect chain with headers for each hop.
    /// </summary>
    public List<RedirectHop> RedirectChain { get; set; } = [];

    /// <summary>
    /// Content analysis results (meta tags, spam words, hidden CSS).
    /// </summary>
    public ContentAnalysis Analysis { get; set; } = new();

    /// <summary>
    /// VirusTotal scan URL for the requested domain.
    /// </summary>
    public string VirusTotalUrl { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the scan.
    /// </summary>
    public DateTime ScannedAtUtc { get; set; }
}

public static class UserAgentPresets
{
    public static readonly Dictionary<string, string> Agents = new(StringComparer.OrdinalIgnoreCase)
    {
        ["googlebot-smartphone"] = "Mozilla/5.0 (Linux; Android 6.0.1; Nexus 5X Build/MMB29P) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.6778.69 Mobile Safari/537.36 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)",
        ["googlebot-desktop"] = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)",
        ["bingbot"] = "Mozilla/5.0 (compatible; bingbot/2.0; +http://www.bing.com/bingbot.htm)",
        ["chrome"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
        ["firefox"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0",
        ["edge"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36 Edg/131.0.0.0"
    };
}

public static class ReferrerPresets
{
    public static readonly Dictionary<string, string> Referrers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["google"] = "https://www.google.com/",
        ["bing"] = "https://www.bing.com/",
        ["yahoo"] = "https://search.yahoo.com/",
        ["none"] = ""
    };
}
