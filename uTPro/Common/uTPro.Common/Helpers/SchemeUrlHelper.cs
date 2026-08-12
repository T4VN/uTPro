namespace uTPro.Extension
{
    public static class SchemeUrlHelper
    {
        private const string PlaceholderHost = "invalid.utpro.local";
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
