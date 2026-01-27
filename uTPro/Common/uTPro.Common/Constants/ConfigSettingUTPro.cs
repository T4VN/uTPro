namespace uTPro.Common.Constants
{
    public struct ConfigSettingUTPro
    {
        public const string KeyPath = "uTPro";
        public const string DefaultCulture = KeyPath + ":DefaultCulture";
        public struct Backoffice
        {
            public const string Key = KeyPath + ":Backoffice";
            public const string Enabled = Key + ":Enabled";
            public const string Domain = Key + ":Url";
        }

        public struct ListRememberLanguage
        {
            public const string Key = KeyPath + ":RememberLanguage";
            public const string Enabled = Key + ":Enabled";
            public struct ListExludeRequestLanguage
            {
                public const string Key = KeyPath + ":ListExludeRequestLanguage";
                public const string Enabled = Key + ":Enabled";
                public const string Paths = Key + ":Paths";
            }
        }
    }
}
