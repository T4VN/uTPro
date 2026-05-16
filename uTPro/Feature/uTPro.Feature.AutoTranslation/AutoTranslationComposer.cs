using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using uTPro.Feature.AutoTranslation.Configuration;
using uTPro.Feature.AutoTranslation.Services;

namespace uTPro.Feature.AutoTranslation;

/// <summary>
/// Registers all Auto Translation services into the DI container.
/// </summary>
public class AutoTranslationComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        // Bind configuration
        builder.Services.Configure<AutoTranslationOptions>(
            builder.Config.GetSection(AutoTranslationOptions.SectionName));

        // Register HttpClient factory (if not already registered)
        builder.Services.AddHttpClient();

        // Register translators
        builder.Services.AddTransient<GoogleFreeTranslator>();
        builder.Services.AddTransient<LibreTranslateTranslator>();
        builder.Services.AddTransient<DeepLTranslator>();

        // Register factory & service
        builder.Services.AddScoped<ITranslatorFactory, TranslatorFactory>();
        builder.Services.AddScoped<IAutoTranslationService, AutoTranslationService>();
    }
}
