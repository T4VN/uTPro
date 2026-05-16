using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using uTPro.Feature.AutoTranslation.Configuration;

namespace uTPro.Feature.AutoTranslation.Services;

/// <summary>
/// Translator backed by a self-hosted or public LibreTranslate instance.
/// </summary>
public class LibreTranslateTranslator : ITranslator
{
    private const string DefaultEndpoint = "https://libretranslate.com/translate";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AutoTranslationOptions _options;
    private readonly ILogger<LibreTranslateTranslator> _logger;

    public LibreTranslateTranslator(
        IHttpClientFactory httpClientFactory,
        IOptions<AutoTranslationOptions> options,
        ILogger<LibreTranslateTranslator> logger)
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

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(LibreTranslateTranslator));
            var payload = new
            {
                q = text,
                source = sourceLang,
                target = targetLang,
                format = isHtml ? "html" : "text",
                api_key = _options.ApiKey ?? string.Empty
            };

            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("translatedText", out var translated))
            {
                return translated.GetString() ?? text;
            }

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LibreTranslate failed for {Source}->{Target}", sourceLang, targetLang);
            return text;
        }
    }

    private static string ToLanguageCode(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return "auto";
        }
        var dash = culture.IndexOf('-');
        return (dash > 0 ? culture[..dash] : culture).ToLowerInvariant();
    }
}
