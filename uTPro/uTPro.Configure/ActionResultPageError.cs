using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Routing;
using uTPro.Extension.CurrentSite;

namespace uTPro.Configure
{
    internal class ActionResultPageError : IActionResult
    {
        private readonly ICurrentSiteExtension _currentSite;
        private int _statusCode;
        private string? _message;
        private string? _title;
        private string? _titlePage;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ActionResultPageError" /> class.
        /// </summary>
        public ActionResultPageError(ICurrentSiteExtension currentSite, string? titlePage = null, string? title = null, string? message = null, int statusCode = StatusCodes.Status404NotFound)
        {
            _currentSite = currentSite;
            _message = message;
            _title = title;
            _titlePage = title;
            _statusCode = statusCode;
        }

        /// <inheritdoc />
        public async Task ExecuteResultAsync(ActionContext context)
        {
            HttpResponse response = context.HttpContext.Response;

            response.Clear();

            response.StatusCode = _statusCode;
            var url = string.Empty;

            if (string.IsNullOrWhiteSpace(_message))
            {
                if (_currentSite.UContext != null)
                {
                    url = WebUtility.HtmlEncode(_currentSite.UContext.OriginalRequestUrl.PathAndQuery);
                    IPublishedRequest? frequest = _currentSite.UContext.PublishedRequest;
                    if (frequest?.HasPublishedContent() == false)
                    {
                        _message = "Not found document matches the URL '{0}'.";
                    }
                    else if (frequest?.HasTemplate() == false)
                    {
                        _message = "No template exists to render the document at URL '{0}'.";
                    }
                }
            }

            context.HttpContext.Items.Add(
                    "message",
                    string.Format(_message ?? "Cannot render the page at URL '{0}'.", url));
            context.HttpContext.Items.Add("title", _title ?? "Welcome to " + _currentSite.GetItem().PageHome?.Value<SeoVisualizer.SeoValues>(nameof(SeoVisualizer.SeoValues))?.Title);
            context.HttpContext.Items.Add("titlePage", _titlePage);

            var pageError = _currentSite.GetItem().PageErrors;
            if (pageError != null)
            {
                //pageError.TemplateId.ToString();

            }
            var viewResult = new ViewResult { ViewName = "globalPageError" };
            await viewResult.ExecuteResultAsync(context);
        }
    }

}
