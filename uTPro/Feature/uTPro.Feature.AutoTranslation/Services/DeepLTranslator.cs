using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using uTPro.Feature.AutoTranslation.Configuration;

namespace uTPro.Feature.AutoTranslation.Services;

/// <summary>
/// Translator backed by the DeepL REST API. Requires <see cref="AutoTranslationOptions.ApiKey"/>.
/// </summary>
public class DeepLTranslator : ITranslator
{
    private const string FreeEndpoint = "https://api-free.deepl.com/v2/translate";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AutoTranslationOptions _options;
    private readonly ILogger<DeepLTranslator> _logger;

    public DeepLTranslator(
        IHttpClientFactory httpClientFactory,
        IOptions<AutoTranslationOptions> options,
        ILogger<DeepLTranslator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> TranslateAsync(string text, string sourceCulture, string targetCulture, bool isHtml, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return text;
        }

        var sourceLang = ToLanguageCode(sourceCulture);
        var targetLang = ToLanguageCode(targetCulture, isTarget: true);

        if (string.Equals(sourceLang, targetLang, StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        var endpoint = string.IsNullOrWhiteSpace(_options.Endpoint) ? FreeEndpoint : _options.Endpoint;

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(DeepLTranslator));

            var form = new List<KeyValuePair<string, string>>
            {
                new("auth_key", _options.ApiKey!),
                new("text", text),
                new("source_lang", sourceLang),
                new("target_lang", targetLang)
            };

            if (isHtml)
            {
                form.Add(new KeyValuePair<string, string>("tag_handling", "html"));
            }

            using var content = new FormUrlEncodedContent(form);
            using var response = await client.PostAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("translations", out var translations) &&
                translations.ValueKind == JsonValueKind.Array &&
                translations.GetArrayLength() > 0 &&
                translations[0].TryGetProperty("text", out var t))
            {
                return t.GetString() ?? text;
            }

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeepL translate failed for {Source}->{Target}", sourceLang, targetLang);
            return text;
        }
    }

    /// <summary>
    /// DeepL accepts ISO 639-1 codes for source and a small set of region-specific codes for target.
    /// </summary>
    private static string ToLanguageCode(string culture, bool isTarget = false)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return string.Empty;
        }

        var upper = culture.ToUpperInvariant();
        var dash = upper.IndexOf('-');
        var primary = dash > 0 ? upper[..dash] : upper;

        if (!isTarget)
        {
            return primary; // DeepL source ignores region.
        }

        // Specific cases where DeepL needs region-aware codes.
        return upper switch
        {
            "EN-US" => "EN-US",
            "EN-GB" => "EN-GB",
            "PT-BR" => "PT-BR",
            "PT-PT" => "PT-PT",
            _ => primary
        };
    }
}
