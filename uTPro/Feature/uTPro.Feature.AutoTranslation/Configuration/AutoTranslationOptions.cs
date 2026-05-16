namespace uTPro.Feature.AutoTranslation.Configuration;

/// <summary>
/// Strongly typed configuration for the Auto Translation feature.
/// Bound from the <c>uTPro:AutoTranslation</c> section of appsettings.json.
/// </summary>
public class AutoTranslationOptions
{
    public const string SectionName = "uTPro:AutoTranslation";

    /// <summary>
    /// Master switch. When false the feature is hidden in the back-office.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Translation provider. Supported values: "Google", "LibreTranslate", "DeepL".
    /// </summary>
    public string Provider { get; set; } = "Google";

    /// <summary>
    /// Optional API key for paid providers (DeepL, Google Cloud, LibreTranslate hosted).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Custom HTTP endpoint. When omitted the provider's default endpoint is used.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Property editor aliases that participate in auto-translation.
    /// </summary>
    public string[] AllowedEditors { get; set; } = new[]
    {
        "Umbraco.TextBox",
        "Umbraco.TextArea",
        "Umbraco.TinyMCE",
        "Umbraco.RichText",
        "Umbraco.Plain.String",
        "Umbraco.Plain.Text",
        "Umbraco.MultipleTextstring",
        "Umbraco.Markdown.Editor",
        "Umbraco.MarkdownEditor"
    };
}
