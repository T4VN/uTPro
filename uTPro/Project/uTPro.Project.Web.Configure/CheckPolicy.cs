using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Composing;

namespace uTPro.Project.Web.Configure
{
    class DIICheckPolicy : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
            => builder.Services.AddScoped<ICheckPolicy, CheckPolicy>();
    }

    public interface ICheckPolicy
    {
        string Check(HttpContext httpContext);
    }

    internal class CheckPolicy : ICheckPolicy
    {
        private struct Policy
        {
            internal const bool isCheck = true;
            internal static readonly IReadOnlyList<string> allow_domain = new List<string>()
            {
                "localhost",
                "*.local",
                "*.t4vn.com"
            };
            internal static readonly DateTime exp_Date = DateTime.MaxValue;
        }

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
            bool check = Policy.exp_Date <= DateTime.Now;
            return check ? $"Your web page has been expired ({Policy.exp_Date})!" : null;
        }

        string? checkDomain(HttpContext httpContext)
        {
            bool check = Policy.allow_domain.Count != 0
                && Policy.allow_domain.Any(x => LikeExpressionToRegexPattern(x).IsMatch(httpContext.Request.Host.Host));
            return !check ? $"Your domain name has been blocked ({httpContext.Request.Host.Host})" : null;
        }

        private Regex LikeExpressionToRegexPattern(String likePattern)
        {
            var replacementToken = "~~~";

            String result = likePattern.Replace("_", replacementToken)
                .Replace("%", ".*");

            result = Regex.Replace(result, @"\[.*" + replacementToken + @".*\]", "_");

            result = result.Replace(replacementToken, ".");

            return new Regex("^" + result + "$", RegexOptions.IgnoreCase);
        }
    }

}
