namespace uTPro.Extension
{
    public static class SchemeUrlHelper
    {
        private const string PlaceholderHost = "invalid.utpro.local";

        /// <summary>
        /// Allowed URL schemes for user-facing links. Only these schemes are considered safe.
        /// </summary>
        private static readonly HashSet<string> SafeSchemes = new(StringComparer.OrdinalIgnoreCase)
        {
            "http", "https", "mailto", "tel"
        };

        /// <summary>
        /// Returns the URL if it uses a safe scheme (http, https, mailto, tel, or is relative).
        /// Returns null for dangerous schemes like javascript:, data:, vbscript:, etc.
        /// </summary>
        public static string? GetSafeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            var trimmed = url.Trim();

            // Relative URLs (starting with / or not containing a scheme) are safe
            if (trimmed.StartsWith('/') || trimmed.StartsWith('#'))
            {
                return trimmed;
            }

            // Check if URL has a scheme
            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex < 0)
            {
                // No scheme — treat as relative
                return trimmed;
            }

            // Extract scheme and validate
            var scheme = trimmed[..colonIndex];
            if (SafeSchemes.Contains(scheme))
            {
                return trimmed;
            }

            // Unsafe scheme (javascript:, data:, vbscript:, etc.)
            return null;
        }

        /// <summary>
        /// Ensures a URL has a scheme, using a placeholder host for root-relative paths.
        /// </summary>
        /// <param name="urlRedirect">The URL or path to normalize.</param>
        /// <param name="schemeDefault">The scheme to add when the URL does not specify one.</param>
        /// <returns>
        /// An empty string for blank input; otherwise, the original HTTP or HTTPS URL,
        /// or the input prefixed with the default scheme.
        /// </returns>
        public static string AddScheme(string urlRedirect, string schemeDefault = "https")
        {
            if (string.IsNullOrWhiteSpace(urlRedirect))
            {
                return string.Empty;
            }

            if (urlRedirect.StartsWith('/'))
            {
                return $"{schemeDefault}://{PlaceholderHost}{urlRedirect}";
            }

            if (urlRedirect.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || urlRedirect.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return urlRedirect;
            }

            return $"{schemeDefault}://{urlRedirect}";
        }
    }
}
