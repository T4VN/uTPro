using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;
using uTPro.Common.Constants;

namespace uTPro.Extension
{
    public static class HtmlSectionBlockExtensions
    {
        public enum Position
        {
            Head,
            BodyTop,
            BodyBottom
        }

        public static IDisposable SetSection(this IHtmlHelper helper, string key, Position position)
        {
            if (helper is null)
            {
                throw new ArgumentNullException(nameof(helper));
            }
            return new SectionBlock(helper, key, position);
        }

        public static IHtmlContent RenderSections(this IHtmlHelper helper, Position position)
        {
            if (helper is null)
            {
                throw new ArgumentNullException(nameof(helper));
            }
            var getsection = GetSectionsList(helper, position);
            var strResult = string.Join(Environment.NewLine, getsection);
            var strInline = strResult.ToInline();
            return new HtmlString(strInline);
        }

        private static IEnumerable<string> GetSectionsList(IHtmlHelper helper, Position position)
        {
            var obj = helper.ViewContext.TempData.Where(x => x.Key.EndsWith(Prefix.PrefixSectionBlock + position));
            foreach (var item in obj)
            {
                yield return item.Value?.ToString() ?? string.Empty;
            }
        }

        private static void SetSectionsList(IHtmlHelper helper, Position position, string key, string content)
        {
            if (!helper.ViewContext.TempData.ContainsKey(key + Prefix.PrefixSectionBlock + position))
            {
                helper.ViewContext.TempData.Add(key + Prefix.PrefixSectionBlock + position, content);
            }
        }

        private class SectionBlock : IDisposable
        {
            private readonly TextWriter _originalWriter;
            private readonly StringWriter _scriptsWriter;
            private readonly ViewContext _viewContext;
            private readonly IHtmlHelper _htmlHelper;
            private readonly string _key;
            private readonly Position _position;

            public SectionBlock(IHtmlHelper htmlHelper, string key, Position position)
            {
                _position = position;
                _key = key;
                _htmlHelper = htmlHelper;
                _viewContext = htmlHelper.ViewContext;
                _originalWriter = _viewContext.Writer;
                _viewContext.Writer = _scriptsWriter = new StringWriter();
            }

            public void Dispose()
            {
                _viewContext.Writer = _originalWriter;
                SetSectionsList(_htmlHelper, _position, _key, _scriptsWriter.ToString());
            }
        }
    }
}
