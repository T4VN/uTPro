using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using uTPro.Feature.UrlViewer.Models;

namespace uTPro.Feature.UrlViewer.Services;

public partial class UrlViewerService : IUrlViewerService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UrlViewerService> _logger;

    private const int MaxContentSize = 10 * 1024 * 1024;
    private const int MaxRedirects = 15;

    public UrlViewerService(IHttpClientFactory httpClientFactory, ILogger<UrlViewerService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<UrlViewerResponse> FetchUrlAsync(UrlViewerRequest request, CancellationToken cancellationToken = default)
    {
        var response = new UrlViewerResponse();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Build the full URL
            string scheme = request.Scheme?.ToLowerInvariant() == "http" ? "http" : "https";
            string url = request.Url.Trim();

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                url = url["http://".Length..];
            else if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                url = url["https://".Length..];

            string fullUrl = $"{scheme}://{url}";
            response.RequestedUrl = fullUrl;

            if (!Uri.TryCreate(fullUrl, UriKind.Absolute, out var uri))
            {
                response.Success = false;
                response.ErrorMessage = $"Invalid URL: {fullUrl}";
                return response;
            }

            if (IsPrivateOrLocalHost(uri))
            {
                response.Success = false;
                response.ErrorMessage = "Fetching private or local addresses is not allowed.";
                return response;
            }

            string userAgent = ResolveUserAgent(request.UserAgent);
            response.UserAgentUsed = userAgent;

            string referrer = ResolveReferrer(request.Referrer);
            response.ReferrerUsed = referrer;

            // VirusTotal URL
            response.VirusTotalUrl = $"https://www.virustotal.com/gui/url/{uri.Host}/detection";

            // Follow redirects manually to capture each hop
            var currentUri = uri;
            int redirectCount = 0;

            while (redirectCount < MaxRedirects)
            {
                var hop = await FetchSingleHopAsync(currentUri, userAgent, referrer, cancellationToken);
                response.RedirectChain.Add(hop);

                bool isRedirect = hop.StatusCode is >= 300 and < 400;
                if (!isRedirect)
                {
                    // Final response - read content
                    response.StatusCode = hop.StatusCode;
                    response.StatusDescription = hop.StatusDescription;
                    response.FinalUrl = currentUri.ToString();
                    response.ContentType = hop.Headers.GetValueOrDefault("Content-Type", string.Empty);

                    // Read body content
                    response.HtmlContent = await FetchBodyAsync(currentUri, userAgent, referrer, cancellationToken);
                    response.ContentLength = response.HtmlContent.Length;
                    break;
                }

                // Follow redirect
                string? location = hop.Headers.GetValueOrDefault("Location");
                if (string.IsNullOrEmpty(location))
                {
                    response.StatusCode = hop.StatusCode;
                    response.StatusDescription = hop.StatusDescription;
                    response.FinalUrl = currentUri.ToString();
                    response.ErrorMessage = "Redirect without Location header.";
                    break;
                }

                // Resolve relative URLs
                if (Uri.TryCreate(currentUri, location, out var nextUri))
                {
                    if (IsPrivateOrLocalHost(nextUri))
                    {
                        response.Success = false;
                        response.ErrorMessage = "Redirect leads to a private/local address.";
                        return response;
                    }
                    currentUri = nextUri;
                }
                else
                {
                    response.ErrorMessage = $"Invalid redirect location: {location}";
                    break;
                }

                redirectCount++;
            }

            if (redirectCount >= MaxRedirects)
            {
                response.Success = false;
                response.ErrorMessage = $"Too many redirects (>{MaxRedirects}).";
                return response;
            }

            // Perform content analysis
            response.Analysis = AnalyzeContent(response.HtmlContent);

            // Cloaking detection: fetch BOTH bot and Chrome at the ORIGINAL URL
            // WITHOUT following redirects, then compare status + title + content
            bool isBotUA = request.UserAgent is "googlebot-smartphone" or "googlebot-desktop" or "bingbot";
            if (isBotUA)
            {
                try
                {
                    var targetUri = new Uri(fullUrl);
                    string chromeUA = UserAgentPresets.Agents["chrome"];

                    // Fetch bot response at original URL (no redirect follow) — single request
                    var (botBody, botStatus) = await FetchNoRedirectAsync(targetUri, userAgent, referrer, cancellationToken);

                    // Fetch Chrome response at original URL (no redirect follow) — single request
                    var (chromeBody, chromeStatus) = await FetchNoRedirectAsync(targetUri, chromeUA, string.Empty, cancellationToken);

                    response.Analysis.Cloaking = CompareBotVsChrome(
                        botBody, chromeBody, "Googlebot", botStatus, chromeStatus);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cloaking check failed for {Url}", response.RequestedUrl);
                    response.Analysis.Cloaking = new CloakingResult
                    {
                        IsCloaked = false,
                        Messages = [$"Cloaking check error: {ex.Message}"]
                    };
                }
            }
            else
            {
                response.Analysis.Cloaking = new CloakingResult { IsCloaked = false };
            }

            response.ScannedAtUtc = DateTime.UtcNow;
            response.Success = true;
        }
        catch (TaskCanceledException)
        {
            response.Success = false;
            response.ErrorMessage = "Request timed out after 30 seconds.";
        }
        catch (HttpRequestException ex)
        {
            response.Success = false;
            response.ErrorMessage = $"HTTP request failed: {ex.Message}";
            _logger.LogWarning(ex, "Failed to fetch URL: {Url}", response.RequestedUrl);
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.ErrorMessage = $"Unexpected error: {ex.Message}";
            _logger.LogError(ex, "Unexpected error fetching URL: {Url}", response.RequestedUrl);
        }
        finally
        {
            stopwatch.Stop();
            response.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        }

        return response;
    }

    /// <summary>
    /// Fetch a single hop (headers only, no auto-redirect).
    /// </summary>
    private async Task<RedirectHop> FetchSingleHopAsync(Uri uri, string userAgent, string referrer, CancellationToken ct)
    {
        var hop = new RedirectHop { Url = uri.ToString() };

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, uri);
        httpRequest.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        if (!string.IsNullOrEmpty(referrer))
            httpRequest.Headers.TryAddWithoutValidation("Referer", referrer);
        httpRequest.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        httpRequest.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.5");
        httpRequest.Headers.TryAddWithoutValidation("Cache-Control", "no-cache, no-store");
        httpRequest.Headers.TryAddWithoutValidation("Pragma", "no-cache");

        using var httpResponse = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

        hop.StatusCode = (int)httpResponse.StatusCode;
        hop.StatusDescription = httpResponse.StatusCode.ToString();

        // Build raw headers string (like Hugo's tool shows)
        var rawHeaders = new StringBuilder();
        rawHeaders.AppendLine($"HTTP/{httpResponse.Version} {hop.StatusCode} {hop.StatusDescription}");

        foreach (var header in httpResponse.Headers)
        {
            string val = string.Join(", ", header.Value);
            hop.Headers[header.Key] = val;
            rawHeaders.AppendLine($"{header.Key}: {val}");
        }
        foreach (var header in httpResponse.Content.Headers)
        {
            string val = string.Join(", ", header.Value);
            hop.Headers[header.Key] = val;
            rawHeaders.AppendLine($"{header.Key}: {val}");
        }

        hop.RawHeaders = rawHeaders.ToString();
        return hop;
    }

    /// <summary>
    /// Fetch the body content of the final URL.
    /// </summary>
    private async Task<string> FetchBodyAsync(Uri uri, string userAgent, string referrer, CancellationToken ct)
    {
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = MaxRedirects,
            AutomaticDecompression = DecompressionMethods.All,
            UseCookies = false // Don't share cookies between bot and Chrome requests
        };
        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30),
            MaxResponseContentBufferSize = MaxContentSize
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, uri);
        httpRequest.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        if (!string.IsNullOrEmpty(referrer))
            httpRequest.Headers.TryAddWithoutValidation("Referer", referrer);
        httpRequest.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        httpRequest.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.5");
        httpRequest.Headers.TryAddWithoutValidation("Cache-Control", "no-cache, no-store");
        httpRequest.Headers.TryAddWithoutValidation("Pragma", "no-cache");

        using var httpResponse = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        var contentStream = await httpResponse.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(contentStream);
        var buffer = new char[MaxContentSize];
        int charsRead = await reader.ReadAsync(buffer.AsMemory(0, MaxContentSize), ct);
        return new string(buffer, 0, charsRead);
    }

    /// <summary>
    /// Analyze HTML content for meta tags, spam words, and hidden CSS.
    /// </summary>
    private static ContentAnalysis AnalyzeContent(string html)
    {
        var analysis = new ContentAnalysis();
        if (string.IsNullOrEmpty(html)) return analysis;

        // 1. Extract meta tags
        analysis.MetaTags = ExtractMetaTags(html);

        // 2. Detect spam/hack words
        analysis.SpamWords = DetectSpamWords(html);

        // 3. Detect CSS that hides elements
        analysis.HiddenCssRules = DetectHiddenCss(html);

        // 4. Detect JavaScript issues
        analysis.JsIssues = DetectJsIssues(html);

        return analysis;
    }

    private static List<MetaTagInfo> ExtractMetaTags(string html)
    {
        var tags = new List<MetaTagInfo>();
        var matches = MetaTagRegex().Matches(html);

        foreach (Match match in matches)
        {
            string name = match.Groups["name"].Value;
            string prop = match.Groups["prop"].Value;
            string content = match.Groups["content"].Value;

            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(prop))
                continue;

            tags.Add(new MetaTagInfo
            {
                Name = !string.IsNullOrEmpty(name) ? name : prop,
                Content = WebUtility.HtmlDecode(content)
            });
        }

        // Also extract <title>
        var titleMatch = TitleRegex().Match(html);
        if (titleMatch.Success)
        {
            tags.Insert(0, new MetaTagInfo
            {
                Name = "title",
                Content = WebUtility.HtmlDecode(titleMatch.Groups[1].Value.Trim())
            });
        }

        return tags;
    }

    /// <summary>
    /// Common spam/hack words often injected into hacked sites (pharma, casino, etc.)
    /// </summary>
    private static readonly string[] SpamKeywords =
    [
        "viagra", "cialis", "pharmacy", "casino", "poker", "gambling",
        "payday loan", "cheap oakley", "louis vuitton", "gucci", "prada",
        "replica watch", "ugg boot", "north face", "nike air",
        "hacked by", "defaced", "web shell",
        "buy cheap", "order online", "discount pill",
        "erectile dysfunction", "weight loss pill",
        "free money", "make money fast", "work from home",
        "adult content", "xxx", "porn",
        "bitcoin generator", "crypto hack",
        "seo service", "backlink service", "link building service",
        "japanese keyword hack", "cloaking detected"
    ];

    private static List<SpamWordMatch> DetectSpamWords(string html)
    {
        var results = new List<SpamWordMatch>();
        string lowerHtml = html.ToLowerInvariant();

        foreach (var keyword in SpamKeywords)
        {
            int count = CountOccurrences(lowerHtml, keyword);
            if (count > 0)
            {
                results.Add(new SpamWordMatch { Word = keyword, Count = count });
            }
        }

        return results.OrderByDescending(r => r.Count).ToList();
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    /// <summary>
    /// Detect CSS rules that hide elements — common cloaking technique.
    /// </summary>
    private static List<string> DetectHiddenCss(string html)
    {
        var rules = new List<string>();

        // Match inline styles with hiding patterns
        var inlineMatches = HiddenInlineStyleRegex().Matches(html);
        foreach (Match m in inlineMatches)
        {
            rules.Add(m.Value.Trim());
        }

        // Match <style> blocks and find hiding rules inside
        var styleBlocks = StyleBlockRegex().Matches(html);
        foreach (Match block in styleBlocks)
        {
            string css = block.Groups[1].Value;
            var cssRules = HiddenCssRuleRegex().Matches(css);
            foreach (Match rule in cssRules)
            {
                string trimmed = rule.Value.Trim();
                if (trimmed.Length <= 500) // Avoid huge blocks
                    rules.Add(trimmed);
            }
        }

        return rules.Distinct().ToList();
    }

    /// <summary>
    /// Detect JavaScript issues by static analysis of the HTML source.
    /// </summary>
    private static List<JsIssue> DetectJsIssues(string html)
    {
        var issues = new List<JsIssue>();
        if (string.IsNullOrEmpty(html)) return issues;

        string lower = html.ToLowerInvariant();

        // ── Security issues ──

        // document.write
        int docWriteCount = CountOccurrences(lower, "document.write(");
        if (docWriteCount > 0)
            issues.Add(new JsIssue
            {
                Severity = "warning",
                Category = "security",
                Message = $"document.write() used {docWriteCount} time(s) — can block rendering and is a security risk",
                Snippet = "document.write("
            });

        // eval()
        int evalCount = CountOccurrences(lower, "eval(");
        if (evalCount > 0)
            issues.Add(new JsIssue
            {
                Severity = "error",
                Category = "security",
                Message = $"eval() used {evalCount} time(s) — dangerous, can execute arbitrary code (XSS risk)",
                Snippet = "eval("
            });

        // innerHTML assignment with concatenation (potential XSS)
        var innerHtmlMatches = InnerHtmlAssignRegex().Matches(html);
        if (innerHtmlMatches.Count > 0)
            issues.Add(new JsIssue
            {
                Severity = "warning",
                Category = "security",
                Message = $"innerHTML assignment found {innerHtmlMatches.Count} time(s) — potential XSS if user input is not sanitized",
                Snippet = "innerHTML ="
            });

        // Inline event handlers (onclick, onerror, onload in HTML attributes)
        var inlineEventMatches = InlineEventHandlerRegex().Matches(html);
        if (inlineEventMatches.Count > 3) // Allow a few, flag if excessive
            issues.Add(new JsIssue
            {
                Severity = "info",
                Category = "security",
                Message = $"{inlineEventMatches.Count} inline event handlers found (onclick, onerror, etc.) — consider using addEventListener instead",
                Snippet = "on*=\"...\""
            });

        // ── Suspicious patterns (often found in hacked sites) ──

        // Base64 encoded JS (atob/btoa with long strings)
        if (lower.Contains("atob(") || lower.Contains("btoa("))
        {
            var atobMatches = AtobRegex().Matches(html);
            if (atobMatches.Count > 0)
                issues.Add(new JsIssue
                {
                    Severity = "warning",
                    Category = "suspicious",
                    Message = $"Base64 decode (atob) found {atobMatches.Count} time(s) — often used to obfuscate malicious code",
                    Snippet = "atob('...')"
                });
        }

        // String.fromCharCode with many arguments (obfuscation)
        if (lower.Contains("string.fromcharcode"))
            issues.Add(new JsIssue
            {
                Severity = "warning",
                Category = "suspicious",
                Message = "String.fromCharCode() detected — commonly used to obfuscate malicious payloads",
                Snippet = "String.fromCharCode(...)"
            });

        // unescape with encoded strings
        if (lower.Contains("unescape("))
            issues.Add(new JsIssue
            {
                Severity = "warning",
                Category = "suspicious",
                Message = "unescape() detected — deprecated and often used for code obfuscation",
                Snippet = "unescape("
            });

        // External script from suspicious domains
        var scriptSrcMatches = ScriptSrcRegex().Matches(html);
        foreach (Match m in scriptSrcMatches)
        {
            string src = m.Groups["src"].Value.ToLowerInvariant();
            if (IsSuspiciousScriptDomain(src))
                issues.Add(new JsIssue
                {
                    Severity = "error",
                    Category = "suspicious",
                    Message = $"External script loaded from suspicious source",
                    Snippet = TruncateSnippet(m.Value, 120)
                });
        }

        // ── Deprecated APIs ──

        if (lower.Contains("document.all"))
            issues.Add(new JsIssue
            {
                Severity = "info",
                Category = "deprecated",
                Message = "document.all is deprecated — use standard DOM methods",
                Snippet = "document.all"
            });

        if (lower.Contains("document.layers"))
            issues.Add(new JsIssue
            {
                Severity = "info",
                Category = "deprecated",
                Message = "document.layers is a Netscape-era API — no longer supported",
                Snippet = "document.layers"
            });

        // ── Resource issues ──

        // Mixed content (http:// scripts on https page)
        var httpScripts = HttpScriptRegex().Matches(html);
        if (httpScripts.Count > 0)
            issues.Add(new JsIssue
            {
                Severity = "error",
                Category = "resource",
                Message = $"{httpScripts.Count} script(s) loaded over HTTP (mixed content) — will be blocked by browsers on HTTPS pages",
                Snippet = TruncateSnippet(httpScripts[0].Value, 120)
            });

        // Empty src scripts
        var emptyScripts = EmptyScriptSrcRegex().Matches(html);
        if (emptyScripts.Count > 0)
            issues.Add(new JsIssue
            {
                Severity = "warning",
                Category = "resource",
                Message = $"{emptyScripts.Count} script tag(s) with empty src — causes an extra request to the current page",
                Snippet = "src=\"\""
            });

        // ── Syntax-like issues detectable from source ──

        // Unclosed script tags
        int openScripts = CountOccurrences(lower, "<script");
        int closeScripts = CountOccurrences(lower, "</script>");
        if (openScripts > closeScripts)
            issues.Add(new JsIssue
            {
                Severity = "error",
                Category = "syntax",
                Message = $"Mismatched script tags: {openScripts} opening vs {closeScripts} closing — may cause parsing errors",
                Snippet = $"<script>: {openScripts}, </script>: {closeScripts}"
            });

        // Very long inline scripts (>50KB — possible obfuscated payload)
        var inlineScripts = InlineScriptBlockRegex().Matches(html);
        foreach (Match block in inlineScripts)
        {
            string content = block.Groups[1].Value;
            if (content.Length > 50_000)
                issues.Add(new JsIssue
                {
                    Severity = "warning",
                    Category = "suspicious",
                    Message = $"Very large inline script ({content.Length / 1024}KB) — may contain obfuscated code",
                    Snippet = TruncateSnippet(content, 80)
                });
        }

        return issues;
    }

    private static bool IsSuspiciousScriptDomain(string src)
    {
        // Known suspicious patterns in script URLs
        string[] suspicious = [
            ".tk/", ".ml/", ".ga/", ".cf/", ".gq/",  // Free TLD abuse
            "pastebin.com", "paste.ee", "hastebin.com",
            "bit.ly/", "tinyurl.com/", "t.co/",       // URL shorteners in scripts
            "raw.githubusercontent.com",                // Raw GitHub (can be abused)
            "eval(", "base64",                          // Encoded in URL
        ];
        return suspicious.Any(s => src.Contains(s, StringComparison.OrdinalIgnoreCase));
    }

    private static string TruncateSnippet(string text, int maxLen)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        text = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return text.Length <= maxLen ? text : text[..maxLen] + "...";
    }

    private static string ResolveUserAgent(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return UserAgentPresets.Agents["googlebot-smartphone"];

        return UserAgentPresets.Agents.TryGetValue(key, out var ua)
            ? ua
            : key;
    }

    /// <summary>
    /// Compare content fetched as bot vs Chrome to detect cloaking.
    /// Compares at the original URL level (no redirect follow).
    /// </summary>
    private static CloakingResult CompareBotVsChrome(
        string botHtml, string chromeHtml, string botUaName,
        int botStatus, int chromeStatus)
    {
        var result = new CloakingResult();

        string botTitle = ExtractTitle(botHtml);
        string chromeTitle = ExtractTitle(chromeHtml);

        result.BotTitle = botTitle;
        result.ChromeTitle = chromeTitle;
        result.BotContentLength = botHtml.Length;
        result.ChromeContentLength = chromeHtml.Length;
        result.ContentDifference = Math.Abs(botHtml.Length - chromeHtml.Length);

        // Check title difference
        result.TitleDiffers = !string.Equals(botTitle, chromeTitle, StringComparison.OrdinalIgnoreCase);

        // Check status code difference (e.g. bot gets 302, Chrome gets 200)
        bool statusDiffers = botStatus != chromeStatus;

        // Significant size difference
        bool significantSizeDiff = result.ContentDifference > 500;

        // Determine if cloaked
        result.IsCloaked = result.TitleDiffers || statusDiffers || significantSizeDiff;

        if (result.IsCloaked)
        {
            result.Messages.Add("This site is possibly hacked by a Spam Hack.");

            if (result.TitleDiffers)
            {
                result.Messages.Add(
                    $"When you fetch the page as {botUaName} the title is different from when you fetch the page as Chrome.");
            }

            if (statusDiffers)
            {
                result.Messages.Add(
                    $"The HTTP status code differs: {botUaName} receives {botStatus}, Chrome receives {chromeStatus}.");
            }

            if (significantSizeDiff)
            {
                result.Messages.Add(
                    $"There is a difference of {result.ContentDifference} bytes between the page you serve to Chrome and the version you serve to {botUaName}.");
            }

            result.Messages.Add("You can find information on Fixing the cloaked keywords and links hack.");
        }

        return result;
    }

    private static string ExtractTitle(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        var match = TitleRegex().Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value.Trim()) : string.Empty;
    }

    /// <summary>
    /// Fetch body + status code WITHOUT following redirects — single request per UA.
    /// </summary>
    private static async Task<(string body, int statusCode)> FetchNoRedirectAsync(
        Uri uri, string userAgent, string referrer, CancellationToken ct)
    {
        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.All,
            UseCookies = false
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        req.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        if (!string.IsNullOrEmpty(referrer))
            req.Headers.TryAddWithoutValidation("Referer", referrer);
        req.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        req.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.5");
        req.Headers.TryAddWithoutValidation("Cache-Control", "no-cache, no-store");

        using var resp = await client.SendAsync(req, ct);
        string body = await resp.Content.ReadAsStringAsync(ct);
        return (body, (int)resp.StatusCode);
    }

    private static string ResolveReferrer(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        return ReferrerPresets.Referrers.TryGetValue(key, out var referrer)
            ? referrer
            : key;
    }

    private static bool IsPrivateOrLocalHost(Uri uri)
    {
        var host = uri.Host.ToLowerInvariant();

        if (host is "localhost" or "127.0.0.1" or "::1" or "0.0.0.0")
            return true;

        if (host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            return true;

        if (IPAddress.TryParse(host, out var ip))
        {
            byte[] bytes = ip.GetAddressBytes();
            if (bytes[0] == 10) return true;
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            if (bytes[0] == 169 && bytes[1] == 254) return true;
        }

        return false;
    }

    // --- Compiled Regex patterns ---

    [GeneratedRegex(
        @"<meta\s+[^>]*?(?:(?:name\s*=\s*[""'](?<name>[^""']*)[""'])|(?:property\s*=\s*[""'](?<prop>[^""']*)[""']))[^>]*?content\s*=\s*[""'](?<content>[^""']*)[""'][^>]*/?>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline, matchTimeoutMilliseconds: 5000)]
    private static partial Regex MetaTagRegex();

    [GeneratedRegex(
        @"<title[^>]*>(.*?)</title>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline, matchTimeoutMilliseconds: 5000)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(
        @"style\s*=\s*[""'][^""']*(?:display\s*:\s*none|visibility\s*:\s*hidden|opacity\s*:\s*0|position\s*:\s*absolute[^""']*left\s*:\s*-\d|font-size\s*:\s*0|height\s*:\s*0|width\s*:\s*0|overflow\s*:\s*hidden)[^""']*[""']",
        RegexOptions.IgnoreCase | RegexOptions.Singleline, matchTimeoutMilliseconds: 5000)]
    private static partial Regex HiddenInlineStyleRegex();

    [GeneratedRegex(
        @"<style[^>]*>(.*?)</style>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline, matchTimeoutMilliseconds: 5000)]
    private static partial Regex StyleBlockRegex();

    [GeneratedRegex(
        @"[^{}]*\{[^}]*(?:display\s*:\s*none|visibility\s*:\s*hidden|opacity\s*:\s*0|position\s*:\s*absolute[^}]*left\s*:\s*-\d|font-size\s*:\s*0(?:px)?|height\s*:\s*0(?:px)?|width\s*:\s*0(?:px)?|overflow\s*:\s*hidden)[^}]*\}",
        RegexOptions.IgnoreCase | RegexOptions.Singleline, matchTimeoutMilliseconds: 5000)]
    private static partial Regex HiddenCssRuleRegex();

    // --- JS detection regex patterns ---

    [GeneratedRegex(
        @"\.innerHTML\s*=",
        RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 5000)]
    private static partial Regex InnerHtmlAssignRegex();

    [GeneratedRegex(
        @"\bon\w+\s*=\s*[""']",
        RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 5000)]
    private static partial Regex InlineEventHandlerRegex();

    [GeneratedRegex(
        @"atob\s*\(",
        RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 5000)]
    private static partial Regex AtobRegex();

    [GeneratedRegex(
        @"<script[^>]+src\s*=\s*[""'](?<src>[^""']+)[""']",
        RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 5000)]
    private static partial Regex ScriptSrcRegex();

    [GeneratedRegex(
        @"<script[^>]+src\s*=\s*[""']http://[^""']+[""']",
        RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 5000)]
    private static partial Regex HttpScriptRegex();

    [GeneratedRegex(
        @"<script[^>]+src\s*=\s*[""']\s*[""']",
        RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 5000)]
    private static partial Regex EmptyScriptSrcRegex();

    [GeneratedRegex(
        @"<script[^>]*>(.*?)</script>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline, matchTimeoutMilliseconds: 10000)]
    private static partial Regex InlineScriptBlockRegex();
}
