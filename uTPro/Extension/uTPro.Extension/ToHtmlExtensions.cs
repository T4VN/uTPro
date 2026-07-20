using Microsoft.AspNetCore.Html;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Strings;

namespace uTPro.Extension
{
    public static class ToHtmlExtensions
    {
        // Dangerous tags whose opening/closing markers are neutralised below.
        static readonly IEnumerable<string> lstNotShowHtml = new List<string>()
        {
            "script",
            "style",
            "iframe",
            "object",
            "embed",
            "base",
            "form"
        };

        // NOTE: this is a best-effort neutraliser, NOT a full HTML sanitizer. It strips a few
        // dangerous tags plus inline event handlers and javascript:/vbscript:/data: URIs. Do NOT
        // rely on it as a security boundary for untrusted input — use a real allow-list sanitizer
        // (e.g. Ganss.Xss/HtmlSanitizer) if you must render arbitrary user-supplied HTML.
        static readonly System.Text.RegularExpressions.Regex _eventHandlerRegex =
            new(@"\son\w+\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        static readonly System.Text.RegularExpressions.Regex _dangerousUriRegex =
            new(@"(?:javascript|vbscript|data)\s*:",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

        public static IHtmlContent ToHtml(this IHtmlEncodedString? valueHtml)
        {
            string? value = valueHtml?.ToHtmlString();
            if (value != null)
            {
                value = replaceNotShowHtml(value);
                //value = replaceNewlineToHtml(value);
                //value = replaceDynamicTagEmptyToBr(value, "p", "<br>");
            }
            else
            {
            }
            return new HtmlString(value);
        }

        public static IHtmlContent ToHtml(this string? value)
        {
            if (value != null)
            {
                value = replaceNotShowHtml(value);
                value = replaceNewlineToHtml(value);
                //value = replaceDynamicTagEmptyToBr(value, "p", "<br>");
            }
            else
            {
                value = string.Empty;
            }
            return new HtmlString(value);
        }

        static string replaceNotShowHtml(string value)
        {
            foreach (var item in lstNotShowHtml)
            {
                value = value.Replace($"<{item}", "&lt;", StringComparison.OrdinalIgnoreCase);
                value = value.Replace($"{item}>", "&gt;", StringComparison.OrdinalIgnoreCase);
            }
            // Strip inline event handlers (onclick, onerror, onload, …) and neutralise
            // javascript:/vbscript:/data: URIs so a stray <img onerror=…> / href="javascript:…"
            // can't execute.
            value = _eventHandlerRegex.Replace(value, string.Empty);
            value = _dangerousUriRegex.Replace(value, "blocked:");
            return value;
        }

        static string replaceNewlineToHtml(string value)
        {
            //value = Regex.Replace(value, @"\r\n?|\n", "<br>");
            //value = value.Replace(Environment.NewLine, "<br>");
            value = value.ReplaceLineEndings("<br>");
            return value;
        }

        static string replaceDynamicTagEmptyToBr(string value, string tag, string tabReplace)
        {
            value = Regex.Replace(value,
                $@"<{tag}\s*/>|<{tag}>\s*</{tag}>|<{tag}\s+(?:[^>]*?)\s*>\s*(?:\s*|\n*)<\/{tag}>",
                tabReplace);
            return value;
        }
    }
}
