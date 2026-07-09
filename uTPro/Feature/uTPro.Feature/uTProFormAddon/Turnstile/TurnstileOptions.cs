namespace uTPro.Feature.uTProFormAddon.Turnstile;

/// <summary>
/// Optional global fallback for the Cloudflare Turnstile field, bound from the
/// <c>uTProFormAddon:Turnstile</c> section of appsettings.
///
/// Precedence is always: value entered on the form field (UI) first, then this appsettings
/// value. So a form can override globally-configured keys, and forms that leave a key blank
/// fall back to appsettings.
/// </summary>
public sealed class TurnstileOptions
{
    public const string SectionPath = "uTProFormAddon:Turnstile";

    /// <summary>Public Site Key used to render the widget when the field leaves it blank.</summary>
    public string? SiteKey { get; set; }

    /// <summary>Secret Key used for server-side verification when the field leaves it blank.</summary>
    public string? SecretKey { get; set; }

    /// <summary>Failure message used when the field leaves its Validation Message blank.</summary>
    public string? FailureMessage { get; set; }
}
