using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;

namespace uTPro.Project.Web.Configure
{
    internal class CheckPolicy : ICheckPolicy
    {
        private struct Policy
        {
            internal const bool isCheck = true;
            internal static readonly IReadOnlyList<string> allow_domain = new List<string>()
            {
                "localhost",
                "*.local",
                "*.t4vn.com",
                "*.utpro.dev"
            };
            internal static readonly DateTime exp_Date = DateTime.MaxValue;
        }

        // Cache compiled regex patterns to avoid recompilation on every request
        private static readonly Lazy<IReadOnlyList<Regex>> _compiledPatterns = new(() =>
            Policy.allow_domain.Select(x => LikeExpressionToRegex(x)).ToList());

        public string Check(HttpContext httpContext)
        {
            if (Policy.isCheck)
            {
                return checkExp()
                    ?? checkDomain(httpContext)
                    ?? string.Empty;
            }
            //return string.Empty;
        }

        string? checkExp()
        {
            bool check = Policy.exp_Date <= DateTime.UtcNow;
            return check ? $"Your web page has been expired ({Policy.exp_Date})!" : null;
        }

        string? checkDomain(HttpContext httpContext)
        {
            var host = httpContext.Request.Host.Host;
            bool check = _compiledPatterns.Value.Count != 0
                && _compiledPatterns.Value.Any(x => x.IsMatch(host));
            return !check ? $"Your domain name has been blocked ({host})" : null;
        }

        private static Regex LikeExpressionToRegex(string likePattern)
        {
            // Escape all regex special characters first, then apply wildcard conversions.
            string result = Regex.Escape(likePattern);

            // Convert SQL LIKE-style wildcards:
            //   *  → .*  (zero or more characters, like shell glob)
            //   %  → .*  (zero or more characters)
            //   _  → .   (single character)
            result = result
                .Replace(@"\*", ".*")
                .Replace(@"\%", ".*")
                .Replace(@"\_", ".");

            return new Regex("^" + result + "$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }

}
