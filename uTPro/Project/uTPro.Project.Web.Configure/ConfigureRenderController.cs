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

namespace uTPro.Project.Web.Configure
{
    [ApiExplorerSettings(IgnoreApi = true)]
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
                        if (!EnsurePhysicalViewExists(view))
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering template for {ContentType}", 
                    UmbracoRouteValues?.PublishedRequest?.PublishedContent?.ContentType?.Alias);
                return new ActionResultPageError(_currentSite);
            }
        }
    }
}
