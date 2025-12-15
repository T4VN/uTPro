namespace uTPro.Common.Constants
{
    public struct ConfigCMS_SMTP
    {
        public const string KeyPath = "Umbraco:CMS:Global:Smtp";
        public const string Host = KeyPath + ":Host";
        public const string Port = KeyPath + ":Port";
        public const string From = KeyPath + ":From";
        public const string Username = KeyPath + ":Username";
        public const string Password = KeyPath + ":Password";
        public const string SecureSocketOptions = KeyPath + ":SecureSocketOptions";
        public const string DeliveryMethod = KeyPath + ":DeliveryMethod";
        public static readonly string[] EnableSSL_True = new string[2]
        {
            "Auto","SslOnConnect"
        };
    }
}
