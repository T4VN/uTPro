using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.OutputCaching;
using System.Net.Mime;
using System.Text;
using Umbraco.Cms.Web.Common.Controllers;
using uTPro.Foundation.Sitemap;

namespace uTPro.Project.Web.Controllers.Pages
{
    [Microsoft.AspNetCore.Mvc.ApiExplorerSettings(IgnoreApi = true)]
    public class SitemapPageController : UmbracoPageController
    {
        readonly ISitemapFoundation _foundation;

        public SitemapPageController(
            ILogger<SitemapPageController> logger,
            ICompositeViewEngine compositeViewEngine,
            //
            ISitemapFoundation foundation
            )
        : base(logger, compositeViewEngine)
        {
            _foundation = foundation;
        }

        [Route("/sitemap.xml")]
        [Route("/sitemap")]
        [HttpGet]
        [OutputCache(PolicyName = "Sitemap")]
        [ResponseCache(Duration = 900, Location = ResponseCacheLocation.Any)]
        public IActionResult Index()
        {
            return Content(_foundation.Generate(), MediaTypeNames.Text.Xml, Encoding.UTF8);
        }
    }
}
