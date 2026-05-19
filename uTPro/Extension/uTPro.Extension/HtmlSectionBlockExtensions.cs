using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        // Per-request bucket key. TempData was overkill (serialized to cookie,
        // encrypted, sent on every response). HttpContext.Items lives for one
        // request only and has zero serialization cost.
        private static readonly object _itemsKey = new();

        public static IDisposable SetSection(this IHtmlHelper helper, string key, Position position)
        {
            ArgumentNullException.ThrowIfNull(helper);
            return new SectionBlock(helper, key, position);
        }

        public static IHtmlContent RenderSections(this IHtmlHelper helper, Position position)
        {
            ArgumentNullException.ThrowIfNull(helper);

            var bucket = GetBucket(helper.ViewContext.HttpContext, create: false);
            if (bucket == null || bucket.Count == 0)
            {
                return HtmlString.Empty;
            }

            // Deduplicate content entries per-position. Two blocks that register the
            // same <link> or <script> snippet should only emit once per page.
            var keySuffix = GetSuffix(position);
            HashSet<string>? seen = null;
            System.Text.StringBuilder? sb = null;

            foreach (var (k, v) in bucket)
            {
                if (!k.EndsWith(keySuffix, StringComparison.Ordinal)) continue;
                if (string.IsNullOrEmpty(v)) continue;

                seen ??= new HashSet<string>(StringComparer.Ordinal);
                if (!seen.Add(v)) continue;

                sb ??= new System.Text.StringBuilder();
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(v);
            }

            return sb == null ? HtmlString.Empty : new HtmlString(sb.ToString().ToInline());
        }

        private static Dictionary<string, string>? GetBucket(HttpContext? http, bool create)
        {
            if (http == null) return null;
            if (http.Items.TryGetValue(_itemsKey, out var existing) && existing is Dictionary<string, string> bucket)
            {
                return bucket;
            }
            if (!create) return null;
            bucket = new Dictionary<string, string>(StringComparer.Ordinal);
            http.Items[_itemsKey] = bucket;
            return bucket;
        }

        private static string GetSuffix(Position position) => position switch
        {
            Position.Head => nameof(Position.Head),
            Position.BodyTop => nameof(Position.BodyTop),
            Position.BodyBottom => nameof(Position.BodyBottom),
            _ => position.ToString()
        };

        private static void StoreSection(IHtmlHelper helper, Position position, string key, string content)
        {
            var bucket = GetBucket(helper.ViewContext.HttpContext, create: true);
            if (bucket == null) return;

            // Stored key shape stays the same as before (key + "___" + position) so any
            // external code relying on the storage format still works; RenderSections
            // filters by the position suffix.
            var storageKey = key + Common.Constants.Prefix.PrefixSectionBlock + GetSuffix(position);
            bucket.TryAdd(storageKey, content);
        }

        private sealed class SectionBlock : IDisposable
        {
            private readonly TextWriter _originalWriter;
            private readonly StringWriter _scriptsWriter;
            private readonly ViewContext _viewContext;
            private readonly IHtmlHelper _htmlHelper;
            private readonly string _key;
            private readonly Position _position;
            private bool _disposed;

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
                if (_disposed) return;
                _disposed = true;

                _viewContext.Writer = _originalWriter;
                StoreSection(_htmlHelper, _position, _key, _scriptsWriter.ToString());
                _scriptsWriter.Dispose();
            }
        }
    }
}
