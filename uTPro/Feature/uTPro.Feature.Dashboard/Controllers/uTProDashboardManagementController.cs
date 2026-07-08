using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Extensions;

namespace uTPro.Feature.Dashboard.Controllers;

/// <summary>
/// Backoffice-only (authenticated) endpoints for the uTPro dashboard and header app.
/// Served under <c>/umbraco/management/api/v1/utpro/dashboard/...</c> — never exposed publicly.
/// The GitHub "latest release" lookup is done server-side (cached) so the browser never
/// calls GitHub directly — avoids CORS, corporate proxies and rate-limit issues.
/// </summary>
[VersionedApiBackOfficeRoute("utpro/dashboard")]
[ApiExplorerSettings(GroupName = "uTPro Dashboard")]
public class uTProDashboardManagementController(
    IContentService contentService,
    IMediaService mediaService,
    IUserService userService,
    IMemberService memberService,
    IUmbracoVersion umbracoVersion,
    IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
    ITwoFactorLoginService twoFactorLoginService,
    IScopeProvider scopeProvider,
    IHttpClientFactory httpClientFactory,
    IMemoryCache memoryCache) : ManagementApiControllerBase
{
    private const int ActivityTake = 20;
    private const string GitHubRepo = "T4VN/uTPro";
    private const string LatestVersionCacheKey = "uTPro.Dashboard.LatestVersion";

    // Installed + latest version info. Pass ?refresh=true (used by the "Check for Update"
    // button) to drop the cached GitHub result and re-fetch immediately instead of waiting
    // for the 1-hour cache to expire. Kept as a GET because it's idempotent and only
    // invalidates a cache — no CSRF-sensitive state change, so no antiforgery token needed.
    [HttpGet("version")]
    public Task<IActionResult> Version([FromQuery] bool refresh = false)
    {
        if (refresh)
            memoryCache.Remove(LatestVersionCacheKey);

        return GetVersionResultAsync();
    }

    private async Task<IActionResult> GetVersionResultAsync()
    {
        var installed = NormalizeVersion(GetAppVersion());
        var latest = await GetLatestVersionAsync();

        return Ok(new
        {
            installed,
            latest,
            updateAvailable = !string.IsNullOrEmpty(latest)
                && !string.IsNullOrEmpty(installed)
                && !string.Equals(installed, latest, StringComparison.OrdinalIgnoreCase),
            website = $"https://github.com/{GitHubRepo}",
            releasesUrl = $"https://github.com/{GitHubRepo}/releases",
        });
    }

    [HttpGet("stats")]
    public IActionResult Stats()
    {
        contentService.GetPagedContentInRecycleBin(0, 1, out var contentInBin);
        mediaService.GetPagedMediaInRecycleBin(0, 1, out var mediaInBin);

        var users = userService.GetAll(0, int.MaxValue, out _).ToList();
        var usersTotal = users.Count;
        var usersDisabled = users.Count(u => !u.IsApproved);

        var membersTotal = memberService.GetCount(MemberCountType.All);
        var membersApproved = memberService.GetCount(MemberCountType.Approved);
        var membersDisabled = Math.Max(0, membersTotal - membersApproved);

        return Ok(new
        {
            utproVersion = GetAppVersion(),
            umbracoVersion = umbracoVersion.SemanticVersion.ToString(),
            publishedContent = contentService.CountPublished(),
            contentInRecycleBin = contentInBin,
            mediaInRecycleBin = mediaInBin,
            usersTotal,
            usersDisabled,
            membersTotal,
            membersDisabled,
        });
    }

    // Recent activity across all users (audit trail from umbracoLog, like the Audit Log viewer).
    [HttpGet("recent-activity")]
    public IActionResult RecentActivity() => Ok(GetActivity(null));

    // The current user's own recent activity.
    [HttpGet("my-activity")]
    public IActionResult MyActivity()
    {
        var user = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (user == null) return Unauthorized();
        return Ok(GetActivity(user.Id));
    }

    // Reads the newest audit-log entries from umbracoLog, joined to the user (name) and the
    // node (name), so content/media/language/etc. actions show the affected item's name.
    private IEnumerable<object> GetActivity(int? userId)
    {
        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var syntax = scope.SqlContext.SqlSyntax;

        string L(string c) => "l." + syntax.GetQuotedColumnName(c);
        string U(string c) => "u." + syntax.GetQuotedColumnName(c);
        string N(string c) => "n." + syntax.GetQuotedColumnName(c);

        var sql = scope.SqlContext.Sql()
            .Select($@"{L("Datestamp")} AS Datestamp, {L("userId")} AS UserId,
                       {L("logHeader")} AS LogHeader, {L("logComment")} AS LogComment,
                       {L("entityType")} AS EntityType, {U("userName")} AS UserName,
                       {N("text")} AS NodeName, {N("uniqueId")} AS NodeKey")
            .From($"{syntax.GetQuotedTableName("umbracoLog")} l")
            .LeftJoin($"{syntax.GetQuotedTableName("umbracoUser")} u").On($"{U("id")} = {L("userId")}")
            .LeftJoin($"{syntax.GetQuotedTableName("umbracoNode")} n").On($"{N("id")} = {L("NodeId")}")
            // Skip noise ("Open" = just viewing a node in the backoffice).
            .Where($"{L("logHeader")} <> @0", "Open");

        if (userId.HasValue)
            sql = sql.Where($"{L("userId")} = @0", userId.Value);

        sql = sql.OrderBy($"{L("Datestamp")} DESC, {L("id")} DESC");

        var rows = scope.Database.Page<ActivityRow>(1, ActivityTake, sql).Items;

        return rows.Select(r => new
        {
            date = r.Datestamp,
            user = string.IsNullOrWhiteSpace(r.UserName) ? (r.UserId.HasValue ? $"User {r.UserId}" : "SYSTEM") : r.UserName,
            action = BuildAction(r),
            type = r.LogHeader ?? string.Empty,
            entityType = r.EntityType ?? string.Empty,
            node = r.NodeName ?? string.Empty,
            nodeKey = r.NodeKey,
        }).ToList();
    }

    // Prefer "{Action} {NodeName}" (e.g. "Save Home"); fall back to the log comment
    // (e.g. "Saved languages: en-US, vi-VN") or just the action header.
    private static string BuildAction(ActivityRow r)
    {
        if (!string.IsNullOrWhiteSpace(r.NodeName))
            return $"{r.LogHeader} {r.NodeName}".Trim();
        if (!string.IsNullOrWhiteSpace(r.LogComment))
            return r.LogComment!;
        return r.LogHeader ?? string.Empty;
    }

    private class ActivityRow
    {
        public DateTime Datestamp { get; set; }
        public int? UserId { get; set; }
        public string? LogHeader { get; set; }
        public string? LogComment { get; set; }
        public string? EntityType { get; set; }
        public string? UserName { get; set; }
        public string? NodeName { get; set; }
        public Guid? NodeKey { get; set; }
    }

    [HttpGet("current-user")]
    public async Task<IActionResult> CurrentUser()
    {
        var user = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (user == null) return Unauthorized();

        var providers = await twoFactorLoginService.GetEnabledTwoFactorProviderNamesAsync(user.Key);

        return Ok(new
        {
            name = user.Name,
            email = user.Email,
            lastLoginDate = user.LastLoginDate == default ? (DateTime?)null : user.LastLoginDate,
            lastPasswordChangeDate = user.LastPasswordChangeDate == default ? (DateTime?)null : user.LastPasswordChangeDate,
            twoFactorEnabled = providers.Any(),
        });
    }

    // Latest GitHub release tag, fetched server-side and cached for an hour.
    private async Task<string> GetLatestVersionAsync()
    {
        if (memoryCache.TryGetValue(LatestVersionCacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
            return cached;

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"https://api.github.com/repos/{GitHubRepo}/releases/latest");
            request.Headers.UserAgent.ParseAdd("uTPro-Dashboard");           // GitHub requires a UA
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                if (doc.RootElement.TryGetProperty("tag_name", out var tag))
                {
                    var latest = NormalizeVersion(tag.GetString());
                    if (!string.IsNullOrEmpty(latest))
                        memoryCache.Set(LatestVersionCacheKey, latest, TimeSpan.FromHours(1));
                    return latest;
                }
            }
        }
        catch
        {
            // Offline / rate-limited / blocked by proxy — the UI just falls back to "unknown".
        }

        return string.Empty;
    }

    // "v17.0.4" / "17.0.4+build" -> "17.0.4"
    private static string NormalizeVersion(string? value)
    {
        var v = (value ?? string.Empty).Trim().TrimStart('v', 'V');
        var plus = v.IndexOf('+');
        return plus >= 0 ? v[..plus] : v;
    }

    // The entry assembly is the running web host (uTPro.Project.Web), whose
    // <Version> (e.g. 17.0.4) is the product version we surface.
    private static string GetAppVersion()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        return asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? asm.GetName().Version?.ToString()
            ?? "0.0.0";
    }
}
