using Microsoft.AspNetCore.Http;

namespace uTPro.Extension
{
    public static class SchemeUrlExtensions
    {
        public static string AddScheme(string urlRedirect, string schemeDefault = "https")
        {
            if (urlRedirect.StartsWith("/"))
            {
                return schemeDefault + "://utpro.local" + urlRedirect;
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
