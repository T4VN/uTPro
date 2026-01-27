using Umbraco.Cms.Core.Models.PublishedContent;

namespace uTPro.Extension
{
    public static class ValueToStringExtension
    {
        /// <summary>
        /// Get value to String
        /// </summary>
        /// <param name="value"></param>
        /// <param name="alias"></param>
        /// <param name="culture"></param>
        /// <param name="defaultValue"></param>
        /// <returns>Value string OR default</returns>
        public static string ValueToString(this IPublishedElement value, string alias, string? culture = null, string? defaultValue = null)
        {
            defaultValue = defaultValue ?? string.Empty;
            return (value.Value(alias, culture, defaultValue: defaultValue) ?? defaultValue).ToInline();
        }
    }
}
