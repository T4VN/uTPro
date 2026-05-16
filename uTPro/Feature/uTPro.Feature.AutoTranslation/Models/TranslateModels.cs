namespace uTPro.Feature.AutoTranslation.Models;

/// <summary>
/// Single field that the back-office wants to translate.
/// </summary>
public class TranslateField
{
    public string Alias { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? EditorAlias { get; set; }
}

/// <summary>
/// Body sent from the back-office button when translating in-flight (unsaved) values.
/// </summary>
public class TranslateRequest
{
    /// <summary>
    /// Optional - used to resolve property metadata when EditorAlias is omitted.
    /// </summary>
    public Guid? ItemKey { get; set; }

    /// <summary>
    /// "content" or "media".
    /// </summary>
    public string ItemType { get; set; } = "content";

    /// <summary>
    /// ISO culture (e.g. "en-US"). When null the configured default language is used.
    /// </summary>
    public string? SourceCulture { get; set; }

    /// <summary>
    /// ISO culture of the variant we want to fill (e.g. "vi-VN").
    /// </summary>
    public string TargetCulture { get; set; } = string.Empty;

    public List<TranslateField> Fields { get; set; } = new();
}

/// <summary>
/// Translated values keyed by property alias.
/// </summary>
public class TranslateResult
{
    public string SourceCulture { get; set; } = string.Empty;
    public string TargetCulture { get; set; } = string.Empty;
    public Dictionary<string, string?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Skipped { get; set; } = new();
}
