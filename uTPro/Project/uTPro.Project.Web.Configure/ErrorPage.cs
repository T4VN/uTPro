using System;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace uTPro.Project.Web.Configure
{
    internal class ErrorPage : IContentLastChanceFinder
    {
        private readonly IUmbracoContextFactory _umbracoContextFactory;
        private readonly IPublishedContentQueryAccessor _queryAccessor;

        public ErrorPage(IUmbracoContextFactory umbracoContextFactory, IPublishedContentQueryAccessor queryAccessor)
        {
            _umbracoContextFactory = umbracoContextFactory;
            _queryAccessor = queryAccessor;
        }

        public Task<bool> TryFindContent(IPublishedRequestBuilder request)
        {
            // In the rare case that an umbracoContext cannot be built from the request,
            // we will not be able to find the page
            if (_queryAccessor.TryGetValue(out IPublishedContentQuery? query) && query != null)
            {
                // Find the first notFound page at the root level through the published content cache by its documentTypeAlias
                // You can make this search as complex as you want, you can return different pages based on anything in the original request
                var notFoundPage = query.ContentAtRoot().FirstOrDefault(c => c.ContentType.Alias.Equals(nameof(GlobalPageError), StringComparison.OrdinalIgnoreCase));
                if (notFoundPage != null)
                {
                    //Set the content on the request and mark our search as successful
                    request.SetIs404();
                    request.SetPublishedContent(notFoundPage);
                    return Task.FromResult(request.PublishedContent != null);
                }
            }
            //request.SetIs404();
            request.SetRedirect("/error");
            return Task.FromResult(request.PublishedContent != null);
        }
    }
}
