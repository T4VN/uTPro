using Microsoft.AspNetCore.Http;

namespace uTPro.Extension
{
    public static class HttpRequestIsAjax
    {
        /// <summary>
        /// If a request is an Ajax request.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="httpVerb"></param>
        public static bool IsAjax(this HttpRequest? request, string httpVerb = "")
        {
            if (request == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(httpVerb))
            {
                if (request.Method.ToLower() != httpVerb.ToLower())
                {
                    return false;
                }
            }

            return request.Headers["X-Requested-With"] == "XMLHttpRequest";
        }
    }
}
