namespace uTPro.Common.Constants
{
    public struct ConfigSettingUTPro
    {
        public const string Key = "uTPro";
        public const string DefaultCulture = Key + ":DefaultCulture";
        public struct Backoffice
        {
            public const string Key = ConfigSettingUTPro.Key + ":Backoffice";
            public const string Enabled = Key + ":Enabled";
            public const string Domain = Key + ":Url";
        }

        public struct ListRememberLanguage
        {
            public const string Key = ConfigSettingUTPro.Key + ":RememberLanguage";
            public const string Enabled = Key + ":Enabled";
            public struct ListExludeRequestLanguage
            {
                public const string Key = ListRememberLanguage.Key + ":ListExludeRequestLanguage";
                public const string Enabled = Key + ":Enabled";
                public const string Paths = Key + ":Paths";
            }
        }
    }
}
