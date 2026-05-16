using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using uTPro.Feature.AutoTranslation.Configuration;

namespace uTPro.Feature.AutoTranslation.Services;

/// <summary>
/// Translator that uses the public Google Translate web endpoint (no API key required).
/// Suitable for low-volume back-office translation. For production traffic use the paid API.
/// </summary>
public class GoogleFreeTranslator : ITranslator
{
    private const string DefaultEndpoint = "https://translate.googleapis.com/translate_a/single";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AutoTranslationOptions _options;
    private readonly ILogger<GoogleFreeTranslator> _logger;

    public GoogleFreeTranslator(
        IHttpClientFactory httpClientFactory,
        IOptions<AutoTranslationOptions> options,
        ILogger<GoogleFreeTranslator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> TranslateAsync(string text, string sourceCulture, string targetCulture, bool isHtml, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var sourceLang = ToLanguageCode(sourceCulture);
        var targetLang = ToLanguageCode(targetCulture);

        if (string.Equals(sourceLang, targetLang, StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        var endpoint = string.IsNullOrWhiteSpace(_options.Endpoint) ? DefaultEndpoint : _options.Endpoint;
        var url = $"{endpoint}?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t&q={WebUtility.UrlEncode(text)}";

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(GoogleFreeTranslator));
            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseGoogleResponse(json, fallback: text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google free translate failed for {Source}->{Target}", sourceLang, targetLang);
            return text;
        }
    }

    /// <summary>
    /// Google returns a deeply nested JSON array. The first element contains an array of [translated, original, ...] tuples.
    /// </summary>
    private static string ParseGoogleResponse(string json, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            {
                return fallback;
            }

            var sentences = root[0];
            if (sentences.ValueKind != JsonValueKind.Array)
            {
                return fallback;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var sentence in sentences.EnumerateArray())
            {
                if (sentence.ValueKind == JsonValueKind.Array && sentence.GetArrayLength() > 0)
                {
                    var translated = sentence[0].GetString();
                    if (!string.IsNullOrEmpty(translated))
                    {
                        sb.Append(translated);
                    }
                }
            }

            return sb.Length > 0 ? sb.ToString() : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    /// <summary>
    /// Convert ISO culture (e.g. "en-US") to a language tag understood by the provider ("en").
    /// </summary>
    private static string ToLanguageCode(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return "auto";
        }

        var dash = culture.IndexOf('-');
        var lang = dash > 0 ? culture[..dash] : culture;

        // Google expects "zh-CN" / "zh-TW" rather than just "zh"
        if (lang.Equals("zh", StringComparison.OrdinalIgnoreCase))
        {
            return culture;
        }

        return lang.ToLowerInvariant();
    }
}
