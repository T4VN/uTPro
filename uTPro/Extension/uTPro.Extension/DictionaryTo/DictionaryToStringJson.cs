using Newtonsoft.Json;

namespace uTPro.Extension
{
    public static class DictionaryToStringJsonExtension
    {
        public static string ToJsonString(this IDictionary<string, string> value)
        {
            return JsonConvert.SerializeObject(value, Formatting.None);
        }
    }
}
