using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Cms.Web.Website.Controllers;
using uTPro.Feature.VideoAnalyzer.Configuration;

namespace uTPro.Feature.VideoAnalyzer.Controllers;

/// <summary>
/// Front-end member login/logout for the Video Analyzer page. Members are managed by the
/// maintainer in the backoffice; there is no self-registration by design (quota control).
/// </summary>
public sealed class MemberAuthSurfaceController(
    IUmbracoContextAccessor umbracoContextAccessor,
    IUmbracoDatabaseFactory databaseFactory,
    ServiceContext services,
    AppCaches appCaches,
    IProfilingLogger profilingLogger,
    IPublishedUrlProvider publishedUrlProvider,
    IMemberSignInManager memberSignInManager,
    IOptions<VideoAnalyzerOptions> options)
    : SurfaceController(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
{
    public const string ErrorTempDataKey = "vaLoginError";

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        [FromForm] string? username,
        [FromForm] string? password,
        [FromForm] bool rememberMe,
        [FromForm] string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            TempData[ErrorTempDataKey] = "Please enter your username and password.";
            return CurrentUmbracoPage();
        }

        var result = await memberSignInManager.PasswordSignInAsync(
            username, password, rememberMe, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            TempData[ErrorTempDataKey] = "Invalid username or password.";
            return CurrentUmbracoPage();
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToCurrentUmbracoPage();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await memberSignInManager.SignOutAsync();
        return RedirectToCurrentUmbracoPage();
    }
}
