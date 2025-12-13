using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.Net.Mime;
using System.Text;
using Umbraco.Cms.Web.Common.Controllers;
using uTPro.Foundation.Robots;

namespace uTPro.Project.Web.Controllers.Pages
{
    public class RobotsPageController : UmbracoPageController
    {
        readonly IRobotsFoundation _foundation;

        public RobotsPageController(
            ILogger<RobotsPageController> logger,
            ICompositeViewEngine compositeViewEngine,
            //
            IRobotsFoundation foundation
            )
        : base(logger, compositeViewEngine)
        {
            _foundation = foundation;
        }

        [Route("/robots")]
        [Route("/robots.txt")]
        [HttpGet]
        public IActionResult Index()
        {
            return Content(_foundation.Generate(), MediaTypeNames.Text.Plain, Encoding.UTF8);
        }
    }
}
