using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Web.Common.PublishedModels;
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
            var item = _currentSite.GetItem();
            Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent? pageError = null;

            string titleSite = string.Empty;
            string logo = string.Empty;
            if (item != null)
            {
                pageError = item.PageErrors;
                logo = item.FolderSite.SiteLogo?.Url() ?? string.Empty;
                titleSite = item.FolderSite?.Value<SeoVisualizer.SeoValues>(nameof(GlobalFolderSites.SiteName))?.Title ?? string.Empty;
            }
            context.HttpContext.Items.Add(
                    "message",
                    string.Format(_message ?? "Cannot render the page at URL '{0}'.", url));
            context.HttpContext.Items.Add("title", _title ?? "Welcome to " + titleSite);
            context.HttpContext.Items.Add("titlePage", _titlePage);
            context.HttpContext.Items.Add("logo", logo);

            string? templateError = null;
            if (pageError != null)
            {
                templateError = pageError.GetTemplateAlias();
            }
            var viewResult = new ViewResult { ViewName = templateError ?? "globalPageError" };
            await viewResult.ExecuteResultAsync(context);
        }
    }
}
