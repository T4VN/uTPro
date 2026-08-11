namespace uTPro.Extension
{
    public static class SchemeUrlHelper
    {
        private const string PlaceholderHost = "invalid.utpro.local";
        public static string AddScheme(string urlRedirect, string schemeDefault = "https")
        {
            if (urlRedirect.StartsWith("/"))
            {
                return schemeDefault + $"://{PlaceholderHost}" + urlRedirect;
            }
            if (urlRedirect.StartsWith("http://") || urlRedirect.StartsWith("https://"))
            {
                return urlRedirect;
            }
            else
            {
                return schemeDefault + "://" + urlRedirect;
            }
        }
    }
}
