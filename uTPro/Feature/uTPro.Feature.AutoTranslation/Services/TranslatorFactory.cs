using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using uTPro.Feature.AutoTranslation.Configuration;

namespace uTPro.Feature.AutoTranslation.Services;

public interface ITranslatorFactory
{
    /// <summary>
    /// Resolve the active <see cref="ITranslator"/> based on <see cref="AutoTranslationOptions.Provider"/>.
    /// </summary>
    ITranslator Create();
}

public class TranslatorFactory : ITranslatorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AutoTranslationOptions _options;

    public TranslatorFactory(IServiceProvider serviceProvider, IOptions<AutoTranslationOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    public ITranslator Create()
    {
        var provider = (_options.Provider ?? "Google").Trim();
        return provider.ToLowerInvariant() switch
        {
            "deepl" => _serviceProvider.GetRequiredService<DeepLTranslator>(),
            "libre" or "libretranslate" => _serviceProvider.GetRequiredService<LibreTranslateTranslator>(),
            _ => _serviceProvider.GetRequiredService<GoogleFreeTranslator>(),
        };
    }
}
