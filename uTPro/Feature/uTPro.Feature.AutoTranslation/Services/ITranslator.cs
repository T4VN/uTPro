namespace uTPro.Feature.AutoTranslation.Services;

/// <summary>
/// Abstraction over a translation provider so we can plug Google, DeepL, LibreTranslate, etc.
/// </summary>
public interface ITranslator
{
    /// <summary>
    /// Translate a single string from <paramref name="sourceCulture"/> to <paramref name="targetCulture"/>.
    /// </summary>
    Task<string> TranslateAsync(string text, string sourceCulture, string targetCulture, bool isHtml, CancellationToken cancellationToken = default);
}
