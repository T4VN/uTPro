using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Cms.Web.Common.Routing;
using Umbraco.Cms.Web.Website.Controllers;
using uTPro.Common.Constants;
using uTPro.Extension.CurrentSite;

namespace uTPro.Configure
{
    public static partial class InitConfigure
    {
        public static void AddRenderingDefaults(this IServiceCollection services)
        {
            // Configure Umbraco Render Controller Type
            services?.Configure<UmbracoRenderingDefaultsOptions>(c =>
            {
                c.DefaultControllerType = typeof(ConfigureRenderController);
            });
        }
    }

    public class ConfigureRenderController : RenderController
    {
        readonly ICurrentSiteExtension _currentSite;
        readonly ICompositeViewEngine _compositeViewEngine;
        readonly ICheckPolicy _checkPolicy;
        readonly ILogger<ConfigureRenderController> _logger;
        public ConfigureRenderController(
            ICheckPolicy checkPolicy,
            ICurrentSiteExtension currentSite,
            ILogger<ConfigureRenderController> logger, ICompositeViewEngine compositeViewEngine, IUmbracoContextAccessor umbracoContextAccessor
            )
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _checkPolicy = checkPolicy;
            _currentSite = currentSite;
            _logger = logger;
            _compositeViewEngine = compositeViewEngine;
        }

        protected override IActionResult CurrentTemplate<T>(T model)
        {
            try
            {
                //CurrentPage
                string reasonPolicty = _checkPolicy.Check(HttpContext);
                string nameView = UmbracoRouteValues.PublishedRequest.PublishedContent?.ContentType?.Alias ?? string.Empty;// ?? UmbracoRouteValues.TemplateName ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(nameView) && nameView.Contains(Prefix.PrefixData))
                {
                    nameView = nameView.Split(Prefix.PrefixData)[1];
                }
                string view = "~/Views/" + _currentSite.GetItem().Root.Name + "/" + nameView + ".cshtml";

                if (string.IsNullOrWhiteSpace(reasonPolicty))
                {
                    if (!_compositeViewEngine.GetView(null, view, isMainPage: false).Success)
                    {
                        view = "~/Views/" + UmbracoRouteValues.TemplateName + ".cshtml" ?? string.Empty;
                        if (!EnsurePhsyicalViewExists(view))
                        {
                            if (!_compositeViewEngine.GetView(null, view, isMainPage: false).Success)
                            {
                                // no physical template file was found
                                return new ActionResultPageError(_currentSite);
                            }
                        }
                    }
                }
                else
                {
                    return new ActionResultPageError(_currentSite
                        , title: "PAGE IS BLOCKED"
                        , message: "Please contact admin for more details <br> " + reasonPolicty);
                }
                return View(view, model);
            }
            catch (Exception)
            {
                return new ActionResultPageError(_currentSite);
            }
        }
    }

    [Route("[controller]")]
    public class ErrorController : ControllerBase
    {
        private readonly ICurrentSiteExtension _currentSite;

        public ErrorController(ICurrentSiteExtension currentSite)
        {
            _currentSite = currentSite;
        }

        public IActionResult Index()
        {
            return new ActionResultPageError(_currentSite, titlePage: "PAGE ERROR", message: "Please contact admin for more details", statusCode: StatusCodes.Status400BadRequest);
        }
    }

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
            if (_queryAccessor.TryGetValue(out IPublishedContentQuery query))
            {
                // Find the first notFound page at the root level through the published content cache by its documentTypeAlias
                // You can make this search as complex as you want, you can return different pages based on anything in the original request
                var notFoundPage = query.ContentAtRoot().FirstOrDefault(c => c.ContentType.Alias.Equals(nameof(GlobalPageError), StringComparison.OrdinalIgnoreCase));
                if (notFoundPage != null)
                {
                    //Set the content on the request and mark our search as successful
                    request.SetPublishedContent(notFoundPage);
                    //request.SetResponseStatus(404);
                    return Task.FromResult(true);
                }
            }
            //request.SetIs404();
            request.SetRedirect("/error");
            return Task.FromResult(true);
        }
    }

    // ContentFinders need to be registered into the DI container through a composer
    public class ErrorPageComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.SetContentLastChanceFinder<ErrorPage>();
        }
    }
}
