using Umbraco.Cms.Core.Composing;

namespace uTPro.Project.Web.Configure
{
    // ContentFinders need to be registered into the DI container through a composer
    public class ErrorPageComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.SetContentLastChanceFinder<ErrorPage>();
        }
    }
}
