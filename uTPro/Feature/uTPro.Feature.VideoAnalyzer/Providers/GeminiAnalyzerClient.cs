using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using uTPro.Feature.VideoAnalyzer.Configuration;
using uTPro.Feature.VideoAnalyzer.Models;

namespace uTPro.Feature.VideoAnalyzer.Providers;

/// <summary>Raised when Gemini reports quota exhaustion so the pipeline can degrade gracefully.</summary>
public sealed class GeminiQuotaException(string message) : Exception(message);

/// <summary>Generates the analysis report and powers chat Q&A through the Gemini REST API.</summary>
public interface IGeminiAnalyzer
{
    /// <summary>Primary path: Gemini watches the video itself via its native YouTube-URL input.</summary>
    Task<VideoReport> AnalyzeVideoUrlAsync(string videoId, string language, string metadataContext, CancellationToken cancellationToken);

    /// <summary>Fallback path: text-only analysis over the captured transcript.</summary>
    Task<VideoReport> AnalyzeTranscriptAsync(string transcriptText, string metadataContext, string language, CancellationToken cancellationToken);

    /// <summary>Chat follow-up grounded in the stored report (+ transcript when available).</summary>
    Task<string> ChatAsync(string reportJson, string? transcriptText, IReadOnlyList<(string Role, string Content)> history, string question, CancellationToken cancellationToken);
}

internal sealed class GeminiAnalyzerClient(
    IHttpClientFactory httpClientFactory,
    IOptions<VideoAnalyzerOptions> options,
    ILogger<GeminiAnalyzerClient> logger) : IGeminiAnalyzer
{
    private const string HttpClientName = "uTProVideoAnalyzerGemini";
    private const string ApiBase = "https://generativelanguage.googleapis.com/v1beta/models";

    public async Task<VideoReport> AnalyzeVideoUrlAsync(string videoId, string language, string metadataContext, CancellationToken cancellationToken)
    {
        var prompt = $$"""
            You are a professional video content analyst. Watch the YouTube video referenced below and produce a full analysis.

            {{metadataContext}}

            {{AgeRatingRubric.Text}}

            Return ONLY a JSON object (no markdown fences) with EXACTLY these keys:
            {
              "summary": "3-6 sentence summary of what the video is about",
              "topics": ["3-8 main topics"],
              "mentions": [{ "name": "...", "type": "product|person|place|organization|other", "context": "why/how it is mentioned" }],
              "sentiment": "overall tone in one short sentence",
              "keywords": ["8-15 SEO keywords"],
              "timeline": [{ "startSeconds": 0, "endSeconds": 60, "title": "chapter title", "description": "what happens" }],
              "ageRating": { "label": "P|K|C13|C16|C18", "flags": ["..."], "reasons": ["..."], "confidence": 0.0 },
              "digest": [{ "startSeconds": 0, "text": "what is said/shown around this moment" }]
            }

            Rules:
            - Write every human-readable value in language code "{{language}}".
            - "timeline": 3-12 chapters covering the whole video; seconds from the video start.
            - "digest": 10-40 segments with approximate startSeconds so later questions like
              "what is said at minute 5" can be answered; each text is 1-2 sentences.
            - The ageRating must follow the rubric strictly and cite timestamps in reasons.
            """;

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new { fileData = new { fileUri = $"https://www.youtube.com/watch?v={videoId}" } },
                    },
                },
            },
            generationConfig = new
            {
                temperature = 0.4,
                responseMimeType = "application/json",
            },
        };

        var text = await GenerateAsync(options.Value.GeminiModel, body, cancellationToken);
        return ParseReport(text, options.Value.GeminiModel, contentSource: "ai-video", language);
    }

    public async Task<VideoReport> AnalyzeTranscriptAsync(string transcriptText, string metadataContext, string language, CancellationToken cancellationToken)
    {
        var prompt = $$"""
            You are a professional video content analyst. Analyze the video described by the
            metadata and transcript below (you cannot see the visuals — note that in the report
            by keeping "confidence" of the ageRating conservative).

            {{metadataContext}}

            --- TRANSCRIPT START ---
            {{transcriptText}}
            --- TRANSCRIPT END ---

            {{AgeRatingRubric.Text}}

            Return ONLY a JSON object (no markdown fences) with EXACTLY these keys:
            {
              "summary": "3-6 sentence summary",
              "topics": ["3-8 main topics"],
              "mentions": [{ "name": "...", "type": "product|person|place|organization|other", "context": "..." }],
              "sentiment": "overall tone in one short sentence",
              "keywords": ["8-15 SEO keywords"],
              "timeline": [{ "startSeconds": 0, "endSeconds": 60, "title": "...", "description": "..." }],
              "ageRating": { "label": "P|K|C13|C16|C18", "flags": ["..."], "reasons": ["..."], "confidence": 0.0 },
              "digest": [{ "startSeconds": 0, "text": "..." }]
            }

            Rules: write every human-readable value in language code "{{language}}"; timeline 3-12
            chapters; digest 10-40 segments using transcript timing; the ageRating follows the
            rubric strictly and cites timestamps in reasons.
            """;

        var body = new
        {
            contents = new[] { new { parts = new object[] { new { text = prompt } } } },
            generationConfig = new { temperature = 0.4, responseMimeType = "application/json" },
        };

        var text = await GenerateAsync(options.Value.GeminiModel, body, cancellationToken);
        return ParseReport(text, options.Value.GeminiModel, contentSource: "transcript", language);
    }

    public async Task<string> ChatAsync(
        string reportJson,
        string? transcriptText,
        IReadOnlyList<(string Role, string Content)> history,
        string question,
        CancellationToken cancellationToken)
    {
        var systemText = $"""
            You are a helpful assistant answering questions about ONE specific YouTube video.
            Ground every answer in the analysis report and (when present) the transcript below.
            When the answer relates to a moment in time, cite it as [mm:ss] using the digest or
            transcript timestamps. If the report/transcript does not contain the answer, say so
            honestly instead of inventing. Reply in the same language as the question.

            ANALYSIS REPORT:
            {reportJson}
            """;

        if (!string.IsNullOrWhiteSpace(transcriptText))
        {
            // Keep the context bounded: the head and tail of the transcript carry most signal for Q&A.
            var trimmed = transcriptText.Length <= 120_000
                ? transcriptText
                : transcriptText[..60_000] + "\n...[truncated]...\n" + transcriptText[^60_000..];
            systemText += "\n\nTRANSCRIPT:\n" + trimmed;
        }

        var contents = new List<object>(history.Count + 1);
        foreach (var (role, content) in history)
        {
            contents.Add(new
            {
                role = role == "assistant" ? "model" : "user",
                parts = new object[] { new { text = content } },
            });
        }

        contents.Add(new { role = "user", parts = new object[] { new { text = question } } });

        var body = new
        {
            systemInstruction = new { parts = new object[] { new { text = systemText } } },
            contents,
            generationConfig = new { temperature = 0.6 },
        };

        return (await GenerateAsync(options.Value.GeminiChatModel, body, cancellationToken)).Trim();
    }

    private async Task<string> GenerateAsync(string model, object body, CancellationToken cancellationToken)
    {
        var apiKey = options.Value.GeminiApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("VideoAnalyzer: GeminiApiKey is not configured.");
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        using var response = await client.PostAsJsonAsync($"{ApiBase}/{model}:generateContent", body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if ((int)response.StatusCode == 429
                || errorBody.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase)
                || errorBody.Contains("quota", StringComparison.OrdinalIgnoreCase))
            {
                throw new GeminiQuotaException(Truncate(errorBody, 400));
            }

            logger.LogWarning("VideoAnalyzer: Gemini call failed ({StatusCode}): {Body}",
                (int)response.StatusCode, Truncate(errorBody, 400));
            throw new HttpRequestException($"Gemini request failed with {(int)response.StatusCode}.");
        }

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        // Join all text parts; a blocked or empty response raises with the reason.
        var candidates = payload.GetPropertyOrNull("candidates");
        if (candidates is { } candidateList && candidateList.GetArrayLength() > 0)
        {
            var parts = candidateList[0].GetPropertyOrNull("content")?.GetPropertyOrNull("parts");
            if (parts is { } partList)
            {
                var text = new StringBuilder();
                foreach (var part in partList.EnumerateArray())
                {
                    var piece = part.GetPropertyOrNull("text");
                    if (piece is { } pieceValue && !string.IsNullOrEmpty(pieceValue.GetString()))
                    {
                        text.Append(pieceValue.GetString());
                    }
                }

                return text.ToString();
            }
        }

        var blockReason = payload.GetPropertyOrNull("promptFeedback")?.GetPropertyOrNull("blockReason");
        throw new InvalidOperationException(
            "Gemini returned no content"
            + (blockReason is { } reasonValue ? $" (blocked: {reasonValue.GetString()})." : "."));
    }

    private static VideoReport ParseReport(string text, string modelName, string contentSource, string language)
    {
        var json = StripFences(text);
        var report = JsonSerializer.Deserialize<VideoReport>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new InvalidOperationException("Gemini returned an empty report.");

        report.ModelName = modelName;
        report.ContentSource = contentSource;
        report.Language = language;

        // Defensive normalization: the model sometimes emits negative or unordered seconds.
        foreach (var chapter in report.Timeline)
        {
            chapter.StartSeconds = Math.Max(0, chapter.StartSeconds);
            chapter.EndSeconds = Math.Max(chapter.EndSeconds, chapter.StartSeconds);
        }

        return report;
    }

    private static string StripFences(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewLine >= 0 && lastFence > firstNewLine)
            {
                trimmed = trimmed[(firstNewLine + 1)..lastFence].Trim();
            }
        }

        return trimmed;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "…";
}
