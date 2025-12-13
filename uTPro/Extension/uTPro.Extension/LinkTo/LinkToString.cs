namespace uTPro.Extension
{
    public static class LinkToStringExtension
    {
        public static string LinkToString(this Umbraco.Cms.Core.Models.Link? value)
        {
            if (value != null && !string.IsNullOrWhiteSpace(value?.Url))
            {
                return value.Url ?? "#";
            }
            return "#";
        }
    }
}
