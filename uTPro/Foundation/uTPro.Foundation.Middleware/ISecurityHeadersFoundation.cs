using Microsoft.AspNetCore.Http;

namespace uTPro.Foundation.Middleware
{
    /// <summary>
    /// Resolves the effective set of HTTP security headers for the current request/site.
    /// Driven by the current site's <c>GlobalSecurityHeadersSettings</c> backoffice node
    /// (per-site, editable in the backoffice). When that node is absent or its <c>Enabled</c>
    /// toggle is off, no security headers are emitted. The trusted backoffice host is always
    /// allowed to frame pages (CSP frame-ancestors) so cross-domain backoffice preview keeps working.
    /// </summary>
    public interface ISecurityHeadersFoundation
    {
        /// <summary>
        /// Returns the header name/value pairs to emit, or <c>null</c> when security headers
        /// are disabled for this request/site.
        /// </summary>
        IReadOnlyList<KeyValuePair<string, string>>? Resolve(HttpContext context);
    }
}
