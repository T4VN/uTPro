using Umbraco.Cms.Core.Models.PublishedContent;

namespace uTPro.Extension
{
    public static class ValueToStringArrayExtension
    {
        /// <summary>
        /// Get value to string array
        /// </summary>
        /// <param name="value"></param>
        /// <param name="alias"></param>
        /// <param name="culture"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string[] ValueToStringArray(this IPublishedContent value, string alias, string? culture = null, string[]? defaultValue = null)
        {
            defaultValue = defaultValue ?? new List<string>().ToArray();
            return value.Value<string[]>(alias, culture, defaultValue: defaultValue) ?? defaultValue;
        }
    }
}
