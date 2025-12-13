namespace uTPro.Extension
{
    public static class StringToInlineExtensions
    {
        public static string ToInline(this string? value)
        {
            if (value != null)
            {
                value = value.Replace(Environment.NewLine, " ");
                value = value.Replace("\n", " ");
            }
            else
            {
                value = string.Empty;
            }
            return value;
        }

        public static string ToInline(this Umbraco.Cms.Core.Strings.IHtmlEncodedString? value)
        {
            if (value != null)
            {
                return value.ToString().ToInline();
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
