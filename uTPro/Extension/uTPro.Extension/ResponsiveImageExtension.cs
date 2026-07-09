using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.PublishedModels;
using Umbraco.Extensions;
using uTPro.Extension.CurrentSite;

namespace uTPro.Extension
{
    /// <summary>
    /// Renders a responsive <c>&lt;img&gt;</c> tag driven by the site's
    /// <c>GlobalResponsiveImageSettings</c> (composed onto the Folder Settings node).
    ///
    /// The <c>RepImgTurnOffResponsiveImage</c> toggle ON enables processing: each image URL is
    /// resized (bounded by its longest edge to the sizes configured in "Extend Sizes SrcSet")
    /// and converted to WebP, producing a <c>srcset</c> + <c>sizes</c>. When the list is empty the
    /// defaults 1200 / 1024 / 768 are used. When the toggle is off (or no settings node exists, or
    /// the media is not an image) the original <c>MediaUrl()</c> is emitted untouched. Images are
    /// never upscaled beyond their intrinsic size.
    /// </summary>
    public static class ResponsiveImageExtension
    {
        private const int DefaultDesktop = 1200;
        private const int DefaultTablet = 1024;
        private const int DefaultMobile = 768;
        private const int DefaultQuality = 80;
        private const string Format = "WebP";
        private const string AltTextPropertyAlias = "altText";

        private static readonly Regex _leadingInt = new(@"\d+", RegexOptions.Compiled);

        /// <summary>
        /// Builds a responsive &lt;img&gt; for the given image media item.
        /// </summary>
        /// <param name="image">The image media (IPublishedContent).</param>
        /// <param name="alt">Alt text. When null/empty, the media's <c>altText</c> property is used;
        /// if that is also empty an empty alt (decorative) is rendered.</param>
        /// <param name="cssClass">Optional CSS class.</param>
        /// <param name="sizes">Optional <c>sizes</c> attribute (default <c>100vw</c>).</param>
        /// <param name="lazy">Emit loading="lazy" (default true). Ignored when <paramref name="priority"/> is true.</param>
        /// <param name="priority">For LCP / above-the-fold images: eager load + fetchpriority="high".</param>
        /// <param name="includeDimensions">Emit intrinsic width/height (+ a responsive inline style) to
        /// reserve space and avoid layout shift (CLS) without overflowing the parent (default true).</param>
        /// <param name="quality">WebP quality (default 80).</param>
        public static IHtmlContent ResponsiveImg(
            this IPublishedContent? image,
            string? alt = null,
            string? cssClass = null,
            string? sizes = null,
            bool lazy = true,
            bool priority = false,
            bool includeDimensions = true,
            int quality = DefaultQuality)
        {
            if (image == null)
                return HtmlString.Empty;

            var enc = HtmlEncoder.Default;
            var sb = new StringBuilder("<img");

            var settings = GetSettings();
            var isImage = string.Equals(image.ContentType?.Alias,
                Constants.Conventions.MediaTypes.Image, StringComparison.OrdinalIgnoreCase);
            var responsiveOn = isImage && settings?.RepImgTurnOffResponsiveImage == false;

            var intrinsicWidth = isImage ? image.Value<int>("umbracoWidth") : 0;
            var intrinsicHeight = isImage ? image.Value<int>("umbracoHeight") : 0;

            if (!responsiveOn)
            {
                // Original URL as-is — no resize, no WebP, no srcset.
                sb.Append(" src=\"").Append(enc.Encode(image.MediaUrl() ?? string.Empty)).Append('"');
            }
            else
            {
                var widths = ResolveCandidateWidths(settings, intrinsicWidth, intrinsicHeight);
                var src = CropUrl(image, widths[^1], quality) ?? image.MediaUrl();
                var srcset = string.Join(", ", widths.Select(w => $"{CropUrl(image, w, quality)} {w}w"));

                sb.Append(" src=\"").Append(enc.Encode(src ?? string.Empty)).Append('"');
                sb.Append(" srcset=\"").Append(enc.Encode(srcset)).Append('"');
                sb.Append(" sizes=\"").Append(enc.Encode(string.IsNullOrWhiteSpace(sizes) ? "100vw" : sizes)).Append('"');
            }

            // Intrinsic dimensions for CLS. The inline style keeps the image inside its parent
            // (max-width:100%) and preserves aspect ratio (height:auto), so the width/height
            // attributes never force the element to overflow a narrower container.
            if (includeDimensions && intrinsicWidth > 0 && intrinsicHeight > 0)
            {
                sb.Append(" width=\"").Append(intrinsicWidth).Append('"');
                sb.Append(" height=\"").Append(intrinsicHeight).Append('"');
                sb.Append(" style=\"max-width:100%;height:auto\"");
            }

            var altText = !string.IsNullOrWhiteSpace(alt) ? alt : image.Value<string>(AltTextPropertyAlias);
            sb.Append(" alt=\"").Append(!string.IsNullOrWhiteSpace(altText) ? enc.Encode(altText) : "").Append('"');

            if (!string.IsNullOrWhiteSpace(cssClass))
                sb.Append(" class=\"").Append(enc.Encode(cssClass)).Append('"');

            sb.Append(" decoding=\"async\"");
            if (priority)
                sb.Append(" fetchpriority=\"high\"");
            else if (lazy)
                sb.Append(" loading=\"lazy\"");

            sb.Append(" />");
            return new HtmlString(sb.ToString());
        }

        /// <summary>
        /// Builds the ascending, de-duplicated list of rendered widths (the srcset "w" descriptors)
        /// from the "Extend Sizes SrcSet" list (defaults 1200 / 1024 / 768 when empty).
        /// Each configured size bounds the image's LONGEST edge, so portrait and landscape images
        /// are downscaled by whichever dimension is larger. Sizes larger than the image are clamped
        /// to the intrinsic size — images are never upscaled.
        /// </summary>
        private static IReadOnlyList<int> ResolveCandidateWidths(IGlobalResponsiveImageSettings? s, int intrinsicWidth, int intrinsicHeight)
        {
            var targets = new SortedSet<int>();
            AddParsedSizes(targets, s?.RepImgExtendSizesSrcSet);

            // Default sizes when the list is empty: Desktop 1200, Tablet 1024, Mobile 768.
            if (targets.Count == 0)
            {
                targets.Add(DefaultMobile);
                targets.Add(DefaultTablet);
                targets.Add(DefaultDesktop);
            }

            var longest = Math.Max(intrinsicWidth, intrinsicHeight);
            var widths = new SortedSet<int>();

            foreach (var target in targets)
            {
                int renderedWidth;
                if (longest > 0 && intrinsicWidth > 0)
                {
                    // Clamp to the longest edge (no upscale), then project onto the width axis.
                    var effective = Math.Min(target, longest);
                    renderedWidth = (int)Math.Round((double)intrinsicWidth * effective / longest);
                }
                else
                {
                    // Intrinsic size unknown → fall back to the raw target width.
                    renderedWidth = target;
                }

                if (renderedWidth > 0)
                    widths.Add(renderedWidth);
            }

            if (widths.Count == 0)
                widths.Add(intrinsicWidth > 0 ? intrinsicWidth : DefaultDesktop);

            return widths.ToList();
        }

        // Parses "1200w", "1024px", "768" … into their leading integer and adds it to the set.
        private static void AddParsedSizes(SortedSet<int> set, IEnumerable<string>? raw)
        {
            if (raw == null)
                return;

            foreach (var item in raw)
            {
                var match = _leadingInt.Match(item ?? string.Empty);
                if (match.Success && int.TryParse(match.Value, out var value) && value > 0)
                    set.Add(value);
            }
        }

        private static string? CropUrl(IPublishedContent image, int width, int quality)
            => image.GetCropUrl(
                width: width,
                height: null,
                imageCropMode: Umbraco.Cms.Core.Models.ImageCropMode.Max,
                quality: quality,
                furtherOptions: "&format=" + Format);

        private static IGlobalResponsiveImageSettings? GetSettings()
        {
            try
            {
                var services = HttpContextStatic.Accessor?.HttpContext?.RequestServices;
                var item = services?.GetService<ICurrentItemExtension>();
                return item?.FolderSettings as IGlobalResponsiveImageSettings;
            }
            catch
            {
                // No request/site context (or settings node missing) → use code defaults.
                return null;
            }
        }
    }
}
