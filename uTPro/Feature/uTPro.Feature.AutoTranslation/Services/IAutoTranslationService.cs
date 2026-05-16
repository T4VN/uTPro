using uTPro.Feature.AutoTranslation.Models;

namespace uTPro.Feature.AutoTranslation.Services;

/// <summary>
/// High-level orchestration: read text properties from the default-language version of an item,
/// translate them, and return the translations keyed by property alias.
/// </summary>
public interface IAutoTranslationService
{
    /// <summary>
    /// Translate raw values supplied by the client (the editor sends the values currently shown for the default culture).
    /// Used by the back-office button when the editor has unsaved changes.
    /// </summary>
    Task<TranslateResult> TranslateValuesAsync(TranslateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Translate the persisted default-language values of a content item.
    /// </summary>
    Task<TranslateResult> TranslateContentAsync(Guid contentKey, string targetCulture, string? sourceCulture = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Translate AND save the translated values into the target culture variant of a content item.
    /// </summary>
    Task<TranslateResult> TranslateAndSaveContentAsync(Guid contentKey, string targetCulture, string? sourceCulture = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Translate the persisted default-language values of a media item.
    /// </summary>
    Task<TranslateResult> TranslateMediaAsync(Guid mediaKey, string targetCulture, string? sourceCulture = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Translate a single arbitrary string. Used by Translation section helper.
    /// </summary>
    Task<string> TranslateTextAsync(string text, string targetCulture, string? sourceCulture = null, bool isHtml = false, CancellationToken cancellationToken = default);
}
