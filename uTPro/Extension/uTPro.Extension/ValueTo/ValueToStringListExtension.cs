using Umbraco.Cms.Core.Models.PublishedContent;

namespace uTPro.Extension
{
    public static class ValueToStringListExtension
    {
        /// <summary>
        /// Get value to List<string>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="alias"></param>
        /// <param name="culture"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static IEnumerable<string> ValueToListString(this IPublishedContent value, string alias, string? culture = null, IEnumerable<string>? defaultValue = null)
        {
            defaultValue = defaultValue ?? new List<string>();
            return value.Value(alias, culture, defaultValue: defaultValue) ?? defaultValue;
        }
    }
}
