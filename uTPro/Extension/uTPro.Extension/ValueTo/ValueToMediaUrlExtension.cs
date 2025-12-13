using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace uTPro.Extension
{
    public static class ValueToMediaUrlExtension
    {
        const int QualityDefault = 80;
        const string FormatImgDefault = "WebP";
        public static string ValueToMedia(this IPublishedElement value, string alias, string? culture = null, string? defaultValue = null, string format = FormatImgDefault)
        {
            defaultValue = defaultValue ?? string.Empty;
            return GetUrlMediaFormatCorrect(value.Value<IPublishedContent>(alias, culture), quality: QualityDefault, format: format) ?? defaultValue;
        }

        /// <summary>
        /// Get value to GetCropUrl with format default: WebP
        /// </summary>
        /// <param name="value"></param>
        /// <param name="alias"></param>
        /// <param name="culture"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string ValueToMediaDesktop(this IPublishedElement value, string alias, string? culture = null, string? defaultValue = null)
        {
            defaultValue = defaultValue ?? string.Empty;
            return GetUrlMediaFormatCorrect(value.Value<IPublishedContent>(alias, culture), width: 2048, quality: QualityDefault) ?? defaultValue;
        }

        /// <summary>
        /// Get value to GetCropUrl with format default: WebP
        /// </summary>
        /// <param name="value"></param>
        /// <param name="alias"></param>
        /// <param name="culture"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string ValueToMediaTablet(this IPublishedElement value, string alias, string? culture = null, string? defaultValue = null)
        {
            defaultValue = defaultValue ?? string.Empty;
            return GetUrlMediaFormatCorrect(value.Value<IPublishedContent>(alias, culture), width: 1500, quality: QualityDefault) ?? defaultValue;
        }

        /// <summary>
        /// Get value to GetCropUrl with format default: WebP
        /// </summary>
        /// <param name="value"></param>
        /// <param name="alias"></param>
        /// <param name="culture"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string ValueToMediaMobile(this IPublishedElement value, string alias, string? culture = null, string? defaultValue = null, string? format = null)
        {
            defaultValue = defaultValue ?? string.Empty;
            return GetUrlMediaFormatCorrect(value.Value<IPublishedContent>(alias, culture), width: 800, quality: QualityDefault) ?? defaultValue;
        }

        static string? GetUrlMediaFormatCorrect(IPublishedContent? item
            , int? width = null
            , int? quality = null
            , string format = FormatImgDefault
            )
        {
            if (item == null)
                return null;

            if (true)
            {
                //turn on auto performance media
                switch (item.ContentType.Alias)
                {
                    case Constants.Conventions.MediaTypes.Image:
                        return item.GetCropUrl(
                            width: width
                            , height: null
                            , imageCropMode: Umbraco.Cms.Core.Models.ImageCropMode.Min
                            , quality: quality
                            , furtherOptions: "&format=" + format
                            );
                    default:
                        break;
                }
            }

            //turn off auto performance media
            return item.MediaUrl(mode: UrlMode.Absolute);
        }
    }
}
