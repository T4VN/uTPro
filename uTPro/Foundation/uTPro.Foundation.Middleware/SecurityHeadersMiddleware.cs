using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace uTPro.Foundation.Middleware
{
    /// <summary>
    /// Emits configurable HTTP security headers on frontend HTML responses only. The effective
    /// header set is resolved per-request/per-site by <see cref="ISecurityHeadersFoundation"/>
    /// (per-site <c>GlobalSecurityHeadersSettings</c> backoffice node). Skips the Umbraco backoffice, API, install
    /// and static/plugin paths, and only touches <c>text/html</c> responses so it never
    /// interferes with the backoffice or API payloads.
    /// </summary>
    internal class SecurityHeadersMiddleware
    {
        // Path prefixes that must never receive frontend security headers.
        private static readonly string[] _excludedPathPrefixes =
        {
            "/umbraco",
            "/api",
            "/install",
            "/app_plugins",
            "/mini-profiler-resources"
        };

        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            // Defer header writes until the response is about to start, so the final
            // status code and Content-Type are known and the scope guard is accurate.
            context.Response.OnStarting(state =>
            {
                ApplyHeaders((HttpContext)state);
                return Task.CompletedTask;
            }, context);

            await _next(context).ConfigureAwait(false);
        }

        private static void ApplyHeaders(HttpContext context)
        {
            if (IsExcludedPath(context.Request.Path) || !IsHtmlResponse(context.Response))
                return;

            var foundation = context.RequestServices.GetService<ISecurityHeadersFoundation>();
            var headers = foundation?.Resolve(context);
            if (headers is null || headers.Count == 0)
                return;

            foreach (var header in headers)
                context.Response.Headers[header.Key] = header.Value;
        }

        private static bool IsExcludedPath(PathString path)
        {
            if (!path.HasValue)
                return false;

            return _excludedPathPrefixes.Any(prefix =>
                path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsHtmlResponse(HttpResponse response)
        {
            var contentType = response.ContentType;
            return !string.IsNullOrEmpty(contentType)
                && contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase);
        }
    }
}
