using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;

namespace uTPro.Extension.Search;

/// <summary>
/// Registers <see cref="ISearchExtension"/> in the DI container.
/// </summary>
public sealed class SearchExtensionComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddScoped<ISearchExtension, SearchExtension>();
    }
}
