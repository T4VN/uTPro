using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using uTPro.Extension.CurrentSite;

namespace uTPro.Project.Web.Configure
{
    public class ErrorController : ControllerBase
    {
        private readonly ICurrentSiteExtension _currentSite;

        public ErrorController(ICurrentSiteExtension currentSite)
        {
            _currentSite = currentSite;
        }

        [Route("/error")]
        [Route("/error/{code:int}")]
        public IActionResult Index(int? code = null)
        {
            var statusCode = code ?? StatusCodes.Status404NotFound;
            var (titlePage, message) = statusCode switch
            {
                StatusCodes.Status404NotFound => ("PAGE NOT FOUND", "The page you requested could not be found."),
                StatusCodes.Status500InternalServerError => ("SERVER ERROR", "An unexpected error occurred."),
                StatusCodes.Status403Forbidden => ("ACCESS DENIED", "You do not have permission to view this page."),
                _ => ("PAGE ERROR", "Please contact admin for more details")
            };
            return new ActionResultPageError(_currentSite, titlePage: titlePage, message: message, statusCode: statusCode);
        }
    }
}
