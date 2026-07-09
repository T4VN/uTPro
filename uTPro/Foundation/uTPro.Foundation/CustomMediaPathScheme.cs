using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.IO;

namespace uTPro.Foundation
{
    class CustomMediaPathScheme : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            //Default by umbraco
            builder.Services.AddUnique<IMediaPathScheme, OriginalMediaPathScheme>();
            //builder.Services.AddUnique<IMediaPathScheme, Umbraco.Cms.Core.IO.MediaPathSchemes.TwoGuidsMediaPathScheme>();
            //builder.Services.AddUnique<IMediaPathScheme, Umbraco.Cms.Core.IO.MediaPathSchemes.CombinedGuidsMediaPathScheme>();
            //builder.Services.AddUnique<IMediaPathScheme, Umbraco.Cms.Core.IO.MediaPathSchemes.UniqueMediaPathScheme>();
        }
    }
}
