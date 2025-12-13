using Microsoft.AspNetCore.Html;
using System.Text.RegularExpressions;
using Umbraco.Cms.Core.Strings;

namespace uTPro.Extension
{
    public static class ToHtmlExtensions
    {
        static readonly IEnumerable<string> lstNotShowHtml = new List<string>()
        {
            "script",
            "style"
        };

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
                value = value.Replace($"<{item}", "&lt;");
                value = value.Replace($"{item}>", "&gt;");
            }
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
