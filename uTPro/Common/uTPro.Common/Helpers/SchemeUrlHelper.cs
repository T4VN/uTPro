namespace uTPro.Extension
{
    public static class SchemeUrlHelper
    {
        private const string PlaceholderHost = "invalid.utpro.local";
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
