using Umbraco.Cms.Core;

namespace uTPro.Extension
{
    public static class UdiToGuidExtension
    {
        public static Guid ToGuid(this Udi? value)
        {
            return (value as GuidUdi)?.Guid ?? Guid.Empty;
        }
    }
}
