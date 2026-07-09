using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using uTPro.Feature.SimpleFormBuilder.Models;
using uTPro.Feature.SimpleFormBuilder.Services;

namespace uTPro.Feature.uTProFormAddon.Turnstile;

/// <summary>
/// Wires up the Cloudflare Turnstile form addon — a uTPro-specific extension of the
/// SimpleFormBuilder package that uses only its public extension points (no package edits):
///   • registers a "turnstile" field type — with its own Site Key / Secret Key settings —
///     so it appears in the form builder's picker with dedicated labelled inputs,
///   • registers an HttpClient + a submit-verification handler (IFormSubmissionHandler).
///
/// The widget is rendered by Views/Partials/uTProSimpleForm/Fields/turnstile.cshtml. Keys are
/// entered per form in the field's Site Key / Secret Key settings (stored in the field's
/// Attributes) and fall back to appsettings (uTPro:Feature:Form:Addon:Turnstile) when left blank. The
/// Failure Message reuses the field's built-in Validation Message.
/// </summary>
public sealed class TurnstileComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AdduTProSimpleFormFieldType("turnstile", "Cloudflare Turnstile",
            new SimpleFormFieldAttribute("siteKey", "Site Key", "from appsettings if blank"),
            new SimpleFormFieldAttribute("secretKey", "Secret Key", "from appsettings if blank"));
        builder.Services.AddHttpClient();

        // Optional global fallback keys (used only when a form field leaves them blank).
        builder.Services.Configure<TurnstileOptions>(
            builder.Config.GetSection(TurnstileOptions.SectionPath));

        // Verify the token server-side as a step in the SimpleFormBuilder submission pipeline,
        // before the entry is stored (replaces the previous PostRouting middleware).
        builder.Services.AddSingleton<IFormSubmissionHandler, TurnstileSubmissionHandler>();
    }
}
