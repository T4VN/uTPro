using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.Net.Mime;
using Umbraco.Cms.Web.Common.Controllers;
using uTPro.Foundation.Favicon;

namespace uTPro.Project.Web.Controllers.Pages
{
    public class FaviconPageController : UmbracoPageController
    {
        readonly IFaviconFoundation _foundation;

        public FaviconPageController(
            ILogger<FaviconPageController> logger,
            ICompositeViewEngine compositeViewEngine,
            //
            IFaviconFoundation foundation
            )
        : base(logger, compositeViewEngine)
        {
            _foundation = foundation;
        }

        [Route("/favicon.ico")]
        [Route("/favicon")]
        [ResponseCache(Duration = 900)]
        public IActionResult Index()
        {
            return File(_foundation.Generate(), MediaTypeNames.Image.Icon);
        }
    }
}
