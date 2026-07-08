using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using uTPro.Feature.SimpleFormBuilder.Services;

namespace uTPro.Feature.uTProFormAddon.Turnstile;

/// <summary>
/// Wires up the Cloudflare Turnstile form addon — a uTPro-specific extension of the
/// SimpleFormBuilder package that uses only its public extension points (no package edits):
///   • registers a "turnstile" field type so it appears in the form builder's picker,
///   • registers an HttpClient + the submit-verification middleware.
///
/// The widget is rendered by Views/Partials/uTProSimpleForm/Fields/turnstile.cshtml, and
/// per-form keys are entered through the field's standard settings (see that view).
/// </summary>
public sealed class TurnstileComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AdduTProSimpleFormFieldType("turnstile", "Cloudflare Turnstile");
        builder.Services.AddHttpClient();

        // Optional global fallback keys (used only when a form field leaves them blank).
        builder.Services.Configure<TurnstileOptions>(
            builder.Config.GetSection(TurnstileOptions.SectionPath));

        // Verify the token server-side before the SimpleFormBuilder submit endpoint runs.
        builder.Services.Configure<UmbracoPipelineOptions>(options =>
            options.AddFilter(new UmbracoPipelineFilter(nameof(TurnstileComposer))
            {
                PostRouting = app => app.UseMiddleware<TurnstileValidationMiddleware>()
            }));
    }
}
