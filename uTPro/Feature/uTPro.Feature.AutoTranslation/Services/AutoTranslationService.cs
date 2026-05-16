using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;
using uTPro.Feature.AutoTranslation.Configuration;
using uTPro.Feature.AutoTranslation.Models;

namespace uTPro.Feature.AutoTranslation.Services;

/// <inheritdoc />
public class AutoTranslationService : IAutoTranslationService
{
    private static readonly HashSet<string> HtmlEditors = new(StringComparer.OrdinalIgnoreCase)
    {
        "Umbraco.TinyMCE",
        "Umbraco.RichText",
        "Umbraco.Markdown.Editor",
        "Umbraco.MarkdownEditor"
    };

    private readonly ITranslatorFactory _translatorFactory;
    private readonly ILanguageService _languageService;
    private readonly IContentService _contentService;
    private readonly IMediaService _mediaService;
    private readonly IContentTypeService _contentTypeService;
    private readonly IMediaTypeService _mediaTypeService;
    private readonly AutoTranslationOptions _options;
    private readonly ILogger<AutoTranslationService> _logger;

    public AutoTranslationService(
        ITranslatorFactory translatorFactory,
        ILanguageService languageService,
        IContentService contentService,
        IMediaService mediaService,
        IContentTypeService contentTypeService,
        IMediaTypeService mediaTypeService,
        IOptions<AutoTranslationOptions> options,
        ILogger<AutoTranslationService> logger)
    {
        _translatorFactory = translatorFactory;
        _languageService = languageService;
        _contentService = contentService;
        _mediaService = mediaService;
        _contentTypeService = contentTypeService;
        _mediaTypeService = mediaTypeService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> TranslateTextAsync(string text, string targetCulture, string? sourceCulture = null, bool isHtml = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var source = sourceCulture ?? await GetDefaultCultureAsync();
        var translator = _translatorFactory.Create();
        return await translator.TranslateAsync(text, source, targetCulture, isHtml, cancellationToken);
    }

    public async Task<TranslateResult> TranslateValuesAsync(TranslateRequest request, CancellationToken cancellationToken = default)
    {
        var result = new TranslateResult
        {
            SourceCulture = request.SourceCulture ?? await GetDefaultCultureAsync(),
            TargetCulture = request.TargetCulture
        };

        if (string.IsNullOrWhiteSpace(request.TargetCulture) ||
            string.Equals(result.SourceCulture, request.TargetCulture, StringComparison.OrdinalIgnoreCase))
        {
            return result;
        }

        // Resolve property -> editor alias map for the item (so we know which fields are HTML).
        var editorMap = await ResolveEditorAliasesAsync(request);
        var translator = _translatorFactory.Create();

        foreach (var field in request.Fields)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var editorAlias = !string.IsNullOrWhiteSpace(field.EditorAlias)
                ? field.EditorAlias!
                : editorMap.TryGetValue(field.Alias, out var alias) ? alias : null;

            if (!IsAllowed(editorAlias))
            {
                result.Skipped.Add(field.Alias);
                result.Values[field.Alias] = field.Value;
                continue;
            }

            if (string.IsNullOrWhiteSpace(field.Value))
            {
                result.Values[field.Alias] = field.Value;
                continue;
            }

            var isHtml = editorAlias != null && HtmlEditors.Contains(editorAlias);
            var translated = await translator.TranslateAsync(field.Value!, result.SourceCulture, result.TargetCulture, isHtml, cancellationToken);
            result.Values[field.Alias] = translated;
        }

        return result;
    }

    public async Task<TranslateResult> TranslateContentAsync(Guid contentKey, string targetCulture, string? sourceCulture = null, CancellationToken cancellationToken = default)
    {
        var result = new TranslateResult
        {
            SourceCulture = sourceCulture ?? await GetDefaultCultureAsync(),
            TargetCulture = targetCulture
        };

        var content = _contentService.GetById(contentKey);
        if (content == null)
        {
            return result;
        }

        var contentType = _contentTypeService.Get(content.ContentTypeId);
        if (contentType == null)
        {
            return result;
        }

        var translator = _translatorFactory.Create();
        foreach (var property in content.Properties)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var propType = contentType.CompositionPropertyTypes.FirstOrDefault(p => p.Alias == property.Alias);
            if (propType == null)
            {
                result.Skipped.Add(property.Alias);
                continue;
            }

            // Skip non-culture-variant properties (they don't need translation)
            if (!propType.VariesByCulture())
            {
                result.Skipped.Add(property.Alias);
                continue;
            }

            // Get the raw value for the source culture
            var rawObj = property.GetValue(culture: result.SourceCulture);
            var rawValue = rawObj as string;

            // If value is not a plain string, try to serialize
            if (rawValue == null && rawObj != null)
            {
                try
                {
                    rawValue = JsonSerializer.Serialize(rawObj);
                }
                catch { /* not serializable, skip */ }
            }

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            // Check if this is a BlockGrid/BlockList (JSON with "contentData" or "blocks")
            var editorAlias = propType.PropertyEditorAlias;
            if (IsBlockEditor(editorAlias) && rawValue.TrimStart().StartsWith("{"))
            {
                // Deep translate all text values inside the block JSON
                var translatedJson = await TranslateBlockJsonAsync(rawValue, result.SourceCulture, result.TargetCulture, translator, cancellationToken);
                if (translatedJson != rawValue)
                {
                    result.Values[property.Alias] = translatedJson;
                }
                continue;
            }

            // Check if it's a JSON object (like SeoValues) - translate string values inside
            if (rawValue.TrimStart().StartsWith("{") || rawValue.TrimStart().StartsWith("["))
            {
                var translatedJson = await TranslateJsonValuesAsync(rawValue, result.SourceCulture, result.TargetCulture, translator, cancellationToken);
                if (translatedJson != rawValue)
                {
                    result.Values[property.Alias] = translatedJson;
                }
                continue;
            }

            // Plain text/HTML translation
            var isHtml = HtmlEditors.Contains(editorAlias);
            var translated = await translator.TranslateAsync(rawValue, result.SourceCulture, result.TargetCulture, isHtml, cancellationToken);
            result.Values[property.Alias] = translated;
        }

        return result;
    }

    /// <summary>
    /// Check if the editor is a block-based editor (BlockGrid, BlockList, RichText with blocks).
    /// </summary>
    private static bool IsBlockEditor(string editorAlias)
    {
        return editorAlias.Contains("BlockGrid", StringComparison.OrdinalIgnoreCase) ||
               editorAlias.Contains("BlockList", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(editorAlias, "Umbraco.BlockGrid", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(editorAlias, "Umbraco.BlockList", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Translate all text values inside a BlockGrid/BlockList JSON structure.
    /// Block JSON has "contentData" array with objects containing property values.
    /// </summary>
    private async Task<string> TranslateBlockJsonAsync(string json, string sourceCulture, string targetCulture, ITranslator translator, CancellationToken cancellationToken)
    {
        try
        {
            var node = JsonNode.Parse(json);
            if (node == null) return json;

            bool changed = await TranslateJsonNodeRecursiveAsync(node, sourceCulture, targetCulture, translator, cancellationToken);

            return changed ? node.ToJsonString() : json;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse/translate block JSON");
            return json;
        }
    }

    /// <summary>
    /// Translate string values inside any JSON structure (e.g., SeoValues, complex objects).
    /// </summary>
    private async Task<string> TranslateJsonValuesAsync(string json, string sourceCulture, string targetCulture, ITranslator translator, CancellationToken cancellationToken)
    {
        try
        {
            var node = JsonNode.Parse(json);
            if (node == null) return json;

            bool changed = await TranslateJsonNodeRecursiveAsync(node, sourceCulture, targetCulture, translator, cancellationToken);

            return changed ? node.ToJsonString() : json;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse/translate JSON values");
            return json;
        }
    }

    /// <summary>
    /// Recursively walk a JSON tree and translate all string values that look like human-readable text.
    /// </summary>
    private async Task<bool> TranslateJsonNodeRecursiveAsync(JsonNode node, string sourceCulture, string targetCulture, ITranslator translator, CancellationToken cancellationToken)
    {
        bool changed = false;

        if (node is JsonObject obj)
        {
            var keys = obj.Select(p => p.Key).ToList();
            foreach (var key in keys)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var child = obj[key];
                if (child is JsonValue val && val.TryGetValue<string>(out var strVal))
                {
                    // Only translate if it looks like human-readable text
                    if (ShouldTranslateValue(key, strVal))
                    {
                        var isHtml = strVal.Contains("<") && strVal.Contains(">");
                        var translated = await translator.TranslateAsync(strVal, sourceCulture, targetCulture, isHtml, cancellationToken);
                        if (translated != strVal)
                        {
                            obj[key] = translated;
                            changed = true;
                        }
                    }
                }
                else if (child != null)
                {
                    if (await TranslateJsonNodeRecursiveAsync(child, sourceCulture, targetCulture, translator, cancellationToken))
                        changed = true;
                }
            }
        }
        else if (node is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                var item = arr[i];
                if (item != null)
                {
                    if (await TranslateJsonNodeRecursiveAsync(item, sourceCulture, targetCulture, translator, cancellationToken))
                        changed = true;
                }
            }
        }

        return changed;
    }

    /// <summary>
    /// Determine if a JSON string value should be translated based on its key and content.
    /// Skip GUIDs, URLs, technical identifiers, etc.
    /// </summary>
    private static bool ShouldTranslateValue(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
            return false;

        // Skip keys that are clearly technical/structural
        var skipKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "contentTypeKey", "settingsTypeKey", "key", "contentUdi", "settingsUdi",
            "udi", "id", "guid", "alias", "editorAlias", "propertyEditorAlias",
            "contentTypeAlias", "mediaKey", "url", "src", "href", "icon",
            "rowSpan", "columnSpan", "areaKey", "gridColumns", "forceLeft",
            "forceRight", "areas", "layout", "Umbraco.BlockGrid", "Umbraco.BlockList",
            "$type", "type", "culture", "segment", "propertyEditorUiAlias"
        };

        if (skipKeys.Contains(key))
            return false;

        // Skip if value looks like a GUID
        if (Guid.TryParse(value, out _))
            return false;

        // Skip if value looks like a URL
        if (value.StartsWith("http://") || value.StartsWith("https://") || value.StartsWith("/"))
            return false;

        // Skip if value looks like a number
        if (double.TryParse(value, out _))
            return false;

        // Skip if value looks like a CSS class or technical string
        if (value.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.') && !value.Contains(' '))
            return false;

        // Skip very short values that are likely codes
        if (value.Length <= 3 && !value.Any(char.IsWhiteSpace))
            return false;

        return true;
    }

    public async Task<TranslateResult> TranslateAndSaveContentAsync(Guid contentKey, string targetCulture, string? sourceCulture = null, CancellationToken cancellationToken = default)
    {
        // First, translate
        var result = await TranslateContentAsync(contentKey, targetCulture, sourceCulture, cancellationToken);

        // Then save the translated values into the content item's target culture
        if (result.Values.Count > 0)
        {
            var content = _contentService.GetById(contentKey);
            if (content != null)
            {
                foreach (var (alias, value) in result.Values)
                {
                    if (value != null)
                    {
                        content.SetValue(alias, value, culture: targetCulture);
                    }
                }

                // Save (not publish - let the user decide when to publish)
                var saveResult = _contentService.Save(content);
                if (!saveResult.Success)
                {
                    _logger.LogWarning("Failed to save translated content for {Key}: {Status}", contentKey, saveResult.Result);
                }
            }
        }

        return result;
    }

    public async Task<TranslateResult> TranslateMediaAsync(Guid mediaKey, string targetCulture, string? sourceCulture = null, CancellationToken cancellationToken = default)
    {
        var result = new TranslateResult
        {
            SourceCulture = sourceCulture ?? await GetDefaultCultureAsync(),
            TargetCulture = targetCulture
        };

        var media = _mediaService.GetById(mediaKey);
        if (media == null)
        {
            return result;
        }

        var mediaType = _mediaTypeService.Get(media.ContentTypeId);
        if (mediaType == null)
        {
            return result;
        }

        var translator = _translatorFactory.Create();
        foreach (var property in media.Properties)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var propType = mediaType.CompositionPropertyTypes.FirstOrDefault(p => p.Alias == property.Alias);
            if (propType == null || !IsAllowed(propType.PropertyEditorAlias))
            {
                result.Skipped.Add(property.Alias);
                continue;
            }

            var rawValue = property.GetValue() as string;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            var isHtml = HtmlEditors.Contains(propType.PropertyEditorAlias);
            var translated = await translator.TranslateAsync(rawValue, result.SourceCulture, result.TargetCulture, isHtml, cancellationToken);
            result.Values[property.Alias] = translated;
        }

        return result;
    }

    /// <summary>
    /// Look up the property type for each requested alias so we can decide whether the value is HTML.
    /// </summary>
    private async Task<Dictionary<string, string>> ResolveEditorAliasesAsync(TranslateRequest request)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!request.ItemKey.HasValue)
        {
            return map;
        }

        IContentTypeComposition? typeBase = null;

        if (string.Equals(request.ItemType, "media", StringComparison.OrdinalIgnoreCase))
        {
            var media = _mediaService.GetById(request.ItemKey.Value);
            if (media != null)
            {
                typeBase = _mediaTypeService.Get(media.ContentTypeId);
            }
        }
        else
        {
            var content = _contentService.GetById(request.ItemKey.Value);
            if (content != null)
            {
                typeBase = _contentTypeService.Get(content.ContentTypeId);
            }
        }

        if (typeBase == null)
        {
            return map;
        }

        foreach (var pt in typeBase.CompositionPropertyTypes)
        {
            map[pt.Alias] = pt.PropertyEditorAlias;
        }

        await Task.CompletedTask;
        return map;
    }

    private bool IsAllowed(string? editorAlias)
    {
        if (string.IsNullOrEmpty(editorAlias))
        {
            // When unknown, only translate if explicitly allowed.
            return false;
        }

        return _options.AllowedEditors.Any(a => string.Equals(a, editorAlias, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> GetDefaultCultureAsync()
    {
        try
        {
            var defaultIso = await _languageService.GetDefaultIsoCodeAsync();
            return string.IsNullOrWhiteSpace(defaultIso) ? "en-US" : defaultIso;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not resolve default ISO code, falling back to en-US");
            return "en-US";
        }
    }
}
