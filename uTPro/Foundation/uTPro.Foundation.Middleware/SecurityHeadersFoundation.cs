using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;
using uTPro.Common.Constants;
using uTPro.Extension.CurrentSite;

namespace uTPro.Foundation.Middleware
{
    /// <summary>
    /// Resolves the effective security headers for the current site, driven entirely by the
    /// per-site <c>GlobalSecurityHeadersSettings</c> node (editable in the backoffice). When
    /// that node is absent or its <c>Enabled</c> toggle is off, no security headers are emitted.
    /// Sensible code defaults (nosniff, SAMEORIGIN, HSTS 1 year) fill in any field left blank on
    /// an enabled node. The trusted backoffice host is always allowed to frame pages via CSP
    /// frame-ancestors so cross-domain backoffice preview keeps working.
    /// </summary>
    internal sealed class SecurityHeadersFoundation(
        ICurrentItemExtension currentItem,
        IConfiguration config,
        ILogger<SecurityHeadersFoundation> logger) : ISecurityHeadersFoundation
    {
        private const string DefaultXContentTypeOptions = "nosniff";
        private const string DefaultXFrameOptions = "SAMEORIGIN";
        private const string DefaultReferrerPolicy = "strict-origin-when-cross-origin";
        private const int DefaultHstsMaxAgeSeconds = 31536000;

        public IReadOnlyList<KeyValuePair<string, string>>? Resolve(HttpContext context)
        {
            try
            {
                // Secure-by-default: even when no GlobalSecurityHeadersSettings node exists (or it
                // is disabled), a safe baseline set of headers is still emitted. An enabled node
                // overrides/extends the baseline. This avoids shipping frontend pages with no
                // security headers at all on a fresh install.
                var node = FindSettingsNode();
                var enabled = node is { Enabled: true };

                var result = new List<KeyValuePair<string, string>>();
                void Add(string name, string? value)
                {
                    var clean = Sanitize(value);
                    if (!string.IsNullOrEmpty(clean))
                        result.Add(new KeyValuePair<string, string>(name, clean));
                }

                // X-Content-Type-Options: node value (when enabled), else the safe default "nosniff".
                Add("X-Content-Type-Options",
                    enabled && !string.IsNullOrWhiteSpace(node!.XContentTypeOptions)
                        ? node.XContentTypeOptions
                        : DefaultXContentTypeOptions);

                // Referrer-Policy: node value when enabled, else a safe default.
                Add("Referrer-Policy",
                    enabled && !string.IsNullOrWhiteSpace(node!.ReferrerPolicy)
                        ? node.ReferrerPolicy
                        : DefaultReferrerPolicy);

                // Permissions-Policy: only when explicitly configured on an enabled node.
                if (enabled)
                    Add("Permissions-Policy", node!.PermissionsPolicy);

                // Framing: allow the trusted backoffice host(s) to frame pages so cross-domain
                // backoffice preview works, while keeping clickjacking protection.
                var backofficeHosts = GetBackofficeHosts();
                var frameAncestors = backofficeHosts.Length > 0
                    ? "frame-ancestors 'self' " + string.Join(' ', backofficeHosts.Select(ToAncestorSource))
                    : null;

                // Always-on clickjacking protection (enforced). Uses CSP frame-ancestors when a
                // backoffice host is configured (so cross-domain preview still works), otherwise
                // X-Frame-Options. X-Frame-Options is omitted when frame-ancestors is used because
                // it cannot express a cross-origin allow and would block the backoffice iframe.
                void AddFramingProtection()
                {
                    if (frameAncestors != null)
                        Add("Content-Security-Policy", frameAncestors);
                    else
                        Add("X-Frame-Options",
                            enabled && !string.IsNullOrWhiteSpace(node!.XFrameOptions)
                                ? node.XFrameOptions
                                : DefaultXFrameOptions);
                }

                var cspClean = enabled ? Sanitize(node!.ContentSecurityPolicyCsp) : null;
                if (string.IsNullOrWhiteSpace(cspClean))
                {
                    AddFramingProtection();
                }
                else
                {
                    var finalCsp = cspClean!;
                    // Never let an explicit CSP lock out backoffice preview.
                    if (frameAncestors != null
                        && !finalCsp.Contains("frame-ancestors", StringComparison.OrdinalIgnoreCase))
                    {
                        finalCsp = finalCsp.TrimEnd(';', ' ') + "; " + frameAncestors;
                    }

                    if (node!.ContentSecurityPolicyReportOnly)
                    {
                        // Keep clickjacking protection ENFORCED while the full policy is only being
                        // tested in report-only mode (report-only headers don't block anything).
                        AddFramingProtection();
                        Add("Content-Security-Policy-Report-Only", finalCsp);
                    }
                    else
                    {
                        Add("Content-Security-Policy", finalCsp);
                    }
                }

                // HSTS: emitted on HTTPS by default (1 year). An enabled node can tune/disable it.
                if (context.Request.IsHttps && (!enabled || node!.HstsEnabled))
                {
                    var maxAge = enabled && node!.HstsMaxAgeSeconds > 0 ? node.HstsMaxAgeSeconds : DefaultHstsMaxAgeSeconds;
                    var hsts = $"max-age={maxAge}";
                    if (enabled && node!.HstsIncludeSubDomains) hsts += "; includeSubDomains";
                    if (enabled && node!.HstsPreload) hsts += "; preload";
                    Add("Strict-Transport-Security", hsts);
                }

                return result.Count > 0 ? result : null;
            }
            catch (InvalidOperationException ex)
            {
                // Fail open (no headers) rather than 500 — a settings/content lookup issue
                // must never take the frontend down.
                logger.LogWarning(ex, "Failed to resolve security headers; skipping for this request.");
                return null;
            }
            catch (ArgumentException ex)
            {
                // Fail open (no headers) rather than 500 — a settings/content lookup issue
                // must never take the frontend down.
                logger.LogWarning(ex, "Failed to resolve security headers; skipping for this request.");
                return null;
            }
        }

        // The GlobalSecurityHeadersSettings mixin may be composed onto the site settings node,
        // the site root, or the current page — try each, first match wins.
        private IGlobalSecurityHeadersSettings? FindSettingsNode()
            => TryRead(() => currentItem.FolderSettings)
               ?? TryRead(() => currentItem.Root)
               ?? TryRead(() => currentItem.Current);

        private static IGlobalSecurityHeadersSettings? TryRead(Func<IPublishedContent?> getter)
        {
            // ObjectDisposedException derives from InvalidOperationException, so a single
            // InvalidOperationException clause covers both (no request/site context available).
            try { return getter() as IGlobalSecurityHeadersSettings; }
            catch (InvalidOperationException) { return null; }
        }

        private string[] GetBackofficeHosts()
        {
            // appsettings.json uses "Domain"; appsettings.Development.json uses "Url"
            // (matches the ConfigSettingUTPro.Backoffice.Domain constant) — check both.
            var value = config.GetSection(ConfigSettingUTPro.Backoffice.Domain)?.Value
                ?? config.GetSection(ConfigSettingUTPro.Backoffice.Key + ":Domain")?.Value;

            return value?.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? [];
        }

        private static string ToAncestorSource(string host)
        {
            host = host.Trim();
            return host.Contains("://", StringComparison.Ordinal) ? host : "https://" + host;
        }

        // Matches any run of whitespace, including CR, LF and tabs.
        private static readonly Regex _whitespace = new(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Normalises a multiline textarea value into a single-line header value: collapses any
        /// run of whitespace (CR/LF/tab/multiple spaces) to a single space and trims. This both
        /// prevents HTTP response splitting / header injection (CWE-113) and cleans up values
        /// that editors may format across several lines (e.g. a CSP with one directive per line).
        /// </summary>
        private static string? Sanitize(string? value)
            => string.IsNullOrEmpty(value)
                ? value
                : _whitespace.Replace(value, " ").Trim();
    }
}
