using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace uTPro.Feature.Dashboard.Controllers;

/// <summary>
/// Backoffice-only (authenticated) endpoints for the uTPro dashboard and header app.
/// Served under <c>/umbraco/management/api/v1/utpro/dashboard/...</c> — never exposed publicly.
/// The GitHub "latest release" lookup is done server-side (cached) so the browser never
/// calls GitHub directly — avoids CORS, corporate proxies and rate-limit issues.
/// </summary>
[VersionedApiBackOfficeRoute("utpro/dashboard")]
[MapToApi(ConfigureDashboardSwaggerGenOptions.ApiName)]
[ApiExplorerSettings(GroupName = "Dashboard")]
public class uTProDashboardManagementController(
    IContentService contentService,
    IContentTypeService contentTypeService,
    ILanguageService languageService,
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

    // How many detail rows the audit-trail list shows (the chart summarises the whole range).
    private const int TrailListTake = 20;
    private const string DefaultTrailRange = "month";

    // How the chart groups events across the selected window.
    private enum TrailBucket { Daily, Weekly, Monthly }

    // Named ranges for the audit-trail chart: window length (days) + bucket granularity.
    private static readonly Dictionary<string, (int Days, TrailBucket Bucket)> TrailRanges =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["week"] = (7, TrailBucket.Daily),
            ["month"] = (30, TrailBucket.Daily),
            ["quarter"] = (90, TrailBucket.Weekly),
            ["year"] = (365, TrailBucket.Monthly),
        };

    private const string GitHubRepo = "T4VN/uTPro";
    private const string LatestVersionCacheKey = "uTPro.Dashboard.LatestVersion";

    // umbracoLog.entityType value Umbraco writes for dictionary item audit entries
    // (Create/Update/Delete DictionaryItem). Its NodeId is the dictionary item's integer id,
    // which maps to cmsDictionary.pk (not umbracoNode.id), so the name/key must be resolved
    // from cmsDictionary instead of umbracoNode.
    private const string DictionaryEntityType = "DictionaryItem";

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

    /// <summary>Body for the "Create Site" endpoint.</summary>
    public sealed class CreateSiteRequest
    {
        public string? Name { get; set; }
    }

    // Document type aliases for the site skeleton created by "Create Site".
    // Use the generated model constants instead of hard-coded strings so a doc-type alias
    // rename (e.g. globalRoot -> globalFolderRoot) is a compile-time change, not a silent break.
    private const string RootDocTypeAlias = GlobalFolderRoot.ModelTypeAlias;
    private const string SitesFolderDocTypeAlias = GlobalFolderSites.ModelTypeAlias;
    private const string NavigationLinkDocTypeAlias = GlobalFolderNavigationLinkForSite.ModelTypeAlias;

    // Super-user id fallback (-1) for the int-based ContentService audit column. Defined locally
    // instead of Constants.Security.SuperUserId (obsolete, removed in Umbraco 18). These endpoints
    // are section-authorized so CurrentUser is effectively always present; the fallback just keeps
    // the call non-null-dependent.
    private const int SuperUserIdFallback = -1;

    /// <summary>
    /// Nodes are saved as drafts (not published) so the editor can build pages under the
    /// structure and publish when ready. Requires Content section access.
    /// </summary>
    [HttpPost("create-site")]
    [Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSite([FromBody] CreateSiteRequest request)
    {
        var name = request?.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { error = "Site name is required." });
        }

        var userId = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Id ?? SuperUserIdFallback;

        // Culture-variant doc types need the name set per language (not the invariant Name),
        // otherwise Save throws "Cannot save content with an empty name" on a multilingual site.
        var languages = await languageService.GetAllAsync();
        var cultures = languages.Select(l => l.IsoCode).ToArray();

        try
        {
            var root = CreateNode(name, Constants.System.Root, RootDocTypeAlias, userId, cultures);
            if (root is null)
            {
                return BadRequest(new { error = $"Document type '{RootDocTypeAlias}' was not found." });
            }

            var sites = CreateNode("Sites", root.Id, SitesFolderDocTypeAlias, userId, cultures);
            if (sites is null)
            {
                return BadRequest(new { error = $"Document type '{SitesFolderDocTypeAlias}' was not found." });
            }

            var nav = CreateNode("Navigation Link", sites.Id, NavigationLinkDocTypeAlias, userId, cultures);
            if (nav is null)
            {
                return BadRequest(new { error = $"Document type '{NavigationLinkDocTypeAlias}' was not found." });
            }

            return Ok(new
            {
                success = true,
                name,
                rootKey = root.Key,
                rootId = root.Id,
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Failed to create the site: " + ex.Message });
        }
    }

    // Creates and saves a single content node, setting the name correctly for invariant OR
    // culture-variant document types. Returns null when the document type doesn't exist.
    private IContent? CreateNode(string name, int parentId, string docTypeAlias, int userId, string[] cultures)
    {
        var contentType = contentTypeService.Get(docTypeAlias);
        if (contentType is null)
        {
            return null;
        }

        var content = contentService.Create(name, parentId, docTypeAlias, userId);

        if (contentType.VariesByCulture() && cultures.Length > 0)
        {
            foreach (var culture in cultures)
            {
                content.SetCultureName(name, culture);
            }
        }
        else
        {
            content.Name = name;
        }

        var result = contentService.Save(content, userId);
        if (!result.Success)
        {
            throw new InvalidOperationException($"Could not save '{name}'.");
        }

        return content;
    }

    private async Task<IActionResult> GetVersionResultAsync()
    {
        var installed = NormalizeVersion(GetAppVersion());
        var latest = await GetLatestVersionAsync();

        return Ok(new
        {
            installed,
            latest,
            // Only offer an update when the latest release is actually NEWER than what's
            // installed. A plain string-inequality check wrongly flagged an update when the
            // installed build was ahead of the latest published release (e.g. 17.5.1 vs 17.5.0).
            updateAvailable = IsUpdateAvailable(installed, latest),
            website = $"https://github.com/{GitHubRepo}",
            releasesUrl = $"https://github.com/{GitHubRepo}/releases",
        });
    }

    [HttpGet("stats")]
    public IActionResult Stats()
    {
        contentService.GetPagedContentInRecycleBin(0, 1, out var contentInBin);
        mediaService.GetPagedMediaInRecycleBin(0, 1, out var mediaInBin);

        // Total users via an aggregate count (no need to materialise every row).
        var usersTotal = userService.GetCount(MemberCountType.All);

        // Count disabled users by their actual state rather than deriving it from
        // "total − approved": that heuristic wrongly flagged the built-in super-admin as
        // disabled (GetCount(Approved) doesn't count it as expected) and also lumps in
        // invited/pending users. GetAll returns the matching total via the out parameter,
        // so we only fetch a single row.
        userService.GetAll(0, 1, out var usersDisabled, "username", Direction.Ascending,
            new[] { UserState.Disabled }, (string[]?)null, (string?)null);

        var membersTotal = memberService.GetCount(MemberCountType.All);
        var membersApproved = memberService.GetCount(MemberCountType.Approved);
        var membersDisabled = Math.Max(0, membersTotal - membersApproved);

        return Ok(new
        {
            utproVersion = GetAppVersion(),
            runtimeVersion = RuntimeInformation.FrameworkDescription,
            umbracoVersion = umbracoVersion.SemanticVersion.ToSemanticStringWithoutBuild(),
            publishedContent = contentService.CountPublished(),
            contentInRecycleBin = contentInBin,
            mediaInRecycleBin = mediaInBin,
            usersTotal,
            usersDisabled,
            membersTotal,
            membersDisabled,
        });
    }

    // True when the current backoffice user is an administrator. Cross-user audit data
    // (everyone's activity + IPs) must be restricted to admins.
    private bool IsAdmin() =>
        backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.IsAdmin() == true;

    // Recent activity across ALL users (audit trail from umbracoLog) — admin only, since it
    // exposes other users' activity. Non-admins should use my-activity.
    [HttpGet("recent-activity")]
    public IActionResult RecentActivity() => IsAdmin() ? Ok(GetActivity(null)) : Forbid();

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
        string D(string c) => "d." + syntax.GetQuotedColumnName(c);

        // Dictionary items are not umbracoNode rows: resolve their name (the dictionary key,
        // e.g. "uTPro.Form.ContactUs.Message") and edit key (guid) from cmsDictionary instead.
        var nodeName = $"CASE WHEN {L("entityType")} = '{DictionaryEntityType}' THEN {D("key")} ELSE {N("text")} END";
        var nodeKey = $"CASE WHEN {L("entityType")} = '{DictionaryEntityType}' THEN {D("id")} ELSE {N("uniqueId")} END";

        var sql = scope.SqlContext.Sql()
            .Select($@"{L("Datestamp")} AS Datestamp, {L("userId")} AS UserId,
                       {L("logHeader")} AS LogHeader, {L("logComment")} AS LogComment,
                       {L("entityType")} AS EntityType, {U("userName")} AS UserName,
                       {nodeName} AS NodeName, {nodeKey} AS NodeKey")
            .From($"{syntax.GetQuotedTableName("umbracoLog")} l")
            .LeftJoin($"{syntax.GetQuotedTableName("umbracoUser")} u").On($"{U("id")} = {L("userId")}")
            .LeftJoin($"{syntax.GetQuotedTableName("umbracoNode")} n").On($"{N("id")} = {L("NodeId")}")
            .LeftJoin($"{syntax.GetQuotedTableName("cmsDictionary")} d").On($"{D("pk")} = {L("NodeId")}")
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

    // Recent audit trail across ALL users (from umbracoAudit — logins, saves, user changes, etc.).
    // Admin only: it exposes other users' activity and source IP addresses.
    // ?range=week|month|quarter|year (default month) selects the chart/detail window.
    [HttpGet("recent-trail")]
    public IActionResult RecentTrail([FromQuery] string? range = null) =>
        IsAdmin() ? Ok(GetAuditTrail(null, range)) : Forbid();

    // The current user's own recent audit trail.
    [HttpGet("my-trail")]
    public IActionResult MyTrail([FromQuery] string? range = null)
    {
        var user = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser;
        if (user == null) return Unauthorized();
        return Ok(GetAuditTrail(user.Id, range));
    }

    // Reads umbracoAudit (the "Audit Trail") for the selected time window and returns both a
    // per-bucket chart series (all events grouped by day/week/month) and the newest detail rows.
    // These are cross-cutting events (sign-in, save, user management) rather than content actions.
    private object GetAuditTrail(int? userId, string? range)
    {
        // Resolve the requested range, falling back to the default when unknown.
        if (string.IsNullOrWhiteSpace(range) || !TrailRanges.TryGetValue(range, out var window))
        {
            range = DefaultTrailRange;
            window = TrailRanges[DefaultTrailRange];
        }

        var to = DateTime.UtcNow;
        var from = to.Date.AddDays(-(window.Days - 1)); // inclusive of today, going back Days-1

        using var scope = scopeProvider.CreateScope(autoComplete: true);
        var syntax = scope.SqlContext.SqlSyntax;

        string A(string c) => "au." + syntax.GetQuotedColumnName(c);
        string P(string c) => "p." + syntax.GetQuotedColumnName(c);

        // 1) Lightweight date-only pull of every event in the window → chart series + total.
        var dateSql = scope.SqlContext.Sql()
            .Select($"{A("eventDateUtc")} AS EventDateUtc")
            .From($"{syntax.GetQuotedTableName("umbracoAudit")} au")
            .Where($"{A("eventDateUtc")} >= @0", from);

        if (userId.HasValue)
            dateSql = dateSql.Where($"{A("performingUserId")} = @0", userId.Value);

        var dates = scope.Database.Fetch<TrailDateRow>(dateSql).Select(r => r.EventDateUtc).ToList();
        var series = BuildTrailSeries(window.Bucket, from, to, dates);

        // 2) Newest N full rows in the window for the detail list.
        var sql = scope.SqlContext.Sql()
            .Select($@"{A("eventDateUtc")} AS EventDateUtc, {A("performingUserId")} AS UserId,
                       {A("eventType")} AS EventType, {A("eventDetails")} AS EventDetails,
                       {A("performingIp")} AS PerformingIp, {A("affectedDetails")} AS AffectedDetails,
                       {A("performingDetails")} AS PerformingDetails, {P("userName")} AS UserName")
            .From($"{syntax.GetQuotedTableName("umbracoAudit")} au")
            .LeftJoin($"{syntax.GetQuotedTableName("umbracoUser")} p").On($"{P("id")} = {A("performingUserId")}")
            .Where($"{A("eventDateUtc")} >= @0", from);

        if (userId.HasValue)
            sql = sql.Where($"{A("performingUserId")} = @0", userId.Value);

        sql = sql.OrderBy($"{A("eventDateUtc")} DESC, {A("id")} DESC");

        var rows = scope.Database.Page<AuditTrailRow>(1, TrailListTake, sql).Items;

        var items = rows.Select(r => new
        {
            date = DateTime.SpecifyKind(r.EventDateUtc, DateTimeKind.Utc),
            user = ResolveUser(r.UserName, r.PerformingDetails, r.UserId),
            type = r.EventType ?? string.Empty,
            details = r.EventDetails ?? string.Empty,
            ip = r.PerformingIp ?? string.Empty,
            affected = r.AffectedDetails ?? string.Empty,
        }).ToList();

        return new
        {
            range,
            from = DateTime.SpecifyKind(from, DateTimeKind.Utc),
            to = DateTime.SpecifyKind(to, DateTimeKind.Utc),
            total = dates.Count,
            series,
            items,
        };
    }

    // Groups the event dates into contiguous buckets (day/week/month) spanning [from, to] so
    // the chart always has a continuous x-axis, even for days/weeks/months with no events.
    private static List<object> BuildTrailSeries(
        TrailBucket bucket, DateTime from, DateTime to, IEnumerable<DateTime> dates)
    {
        var boundaries = new List<(DateTime Start, DateTime End, string Label)>();

        switch (bucket)
        {
            case TrailBucket.Monthly:
                for (var cursor = new DateTime(from.Year, from.Month, 1); cursor <= to; cursor = cursor.AddMonths(1))
                    boundaries.Add((cursor, cursor.AddMonths(1), cursor.ToString("MMM yyyy")));
                break;
            case TrailBucket.Weekly:
                for (var cursor = from.Date; cursor <= to; cursor = cursor.AddDays(7))
                    boundaries.Add((cursor, cursor.AddDays(7), cursor.ToString("dd MMM")));
                break;
            default: // Daily
                for (var cursor = from.Date; cursor <= to.Date; cursor = cursor.AddDays(1))
                    boundaries.Add((cursor, cursor.AddDays(1), cursor.ToString("dd MMM")));
                break;
        }

        var ordered = dates.OrderBy(d => d).ToList();

        return boundaries.Select(b => (object)new
        {
            date = DateTime.SpecifyKind(b.Start, DateTimeKind.Utc),
            label = b.Label,
            count = ordered.Count(d => d >= b.Start && d < b.End),
        }).ToList();
    }

    private static string ResolveUser(string? userName, string? performingDetails, int? userId)
    {
        if (!string.IsNullOrWhiteSpace(userName)) return userName!;
        if (!string.IsNullOrWhiteSpace(performingDetails)) return performingDetails!;
        return userId.HasValue ? $"User {userId}" : "SYSTEM";
    }

    // Single-column projection used for the lightweight chart-series query.
    private class TrailDateRow
    {
        public DateTime EventDateUtc { get; set; }
    }

    private class AuditTrailRow
    {
        public DateTime EventDateUtc { get; set; }
        public int? UserId { get; set; }
        public string? EventType { get; set; }
        public string? EventDetails { get; set; }
        public string? PerformingIp { get; set; }
        public string? AffectedDetails { get; set; }
        public string? PerformingDetails { get; set; }
        public string? UserName { get; set; }
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

    // True only when 'latest' is a strictly newer version than 'installed'. Compares the numeric
    // parts (major.minor.patch), ignoring any pre-release/build suffix. If either value can't be
    // parsed as a version, falls back to a case-insensitive inequality so behaviour is still sane.
    private static bool IsUpdateAvailable(string installed, string latest)
    {
        if (string.IsNullOrEmpty(installed) || string.IsNullOrEmpty(latest))
            return false;

        if (TryParseVersion(installed, out var installedVersion)
            && TryParseVersion(latest, out var latestVersion))
        {
            return latestVersion > installedVersion;
        }

        return !string.Equals(installed, latest, StringComparison.OrdinalIgnoreCase);
    }

    // Parses "17.5.1" / "17.5.1-rc1" -> Version(17.5.1), dropping any pre-release suffix.
    // Fully qualified System.Version to avoid clashing with this controller's Version() action.
    private static bool TryParseVersion(string value, out System.Version version)
    {
        var core = value;
        var dash = core.IndexOf('-');
        if (dash >= 0)
            core = core[..dash];

        return System.Version.TryParse(core, out version!);
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
