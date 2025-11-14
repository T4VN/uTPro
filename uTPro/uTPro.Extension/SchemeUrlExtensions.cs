namespace uTPro.Extension
{
    public static class SchemeUrlExtensions
    {
        public static string AddScheme(string urlRedirect, string schemeDefault = "https")
        {
            if (urlRedirect.StartsWith("/") || urlRedirect.StartsWith("http://") || urlRedirect.StartsWith("https://"))
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
