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

        /// <summary>
        /// Auto translation settings (Content / Media field text).
        /// </summary>
        public struct AutoTranslation
        {
            public const string Key = ConfigSettingUTPro.Key + ":AutoTranslation";
            public const string Enabled = Key + ":Enabled";
            /// <summary>
            /// Provider name: Google (free, no key), LibreTranslate, DeepL.
            /// </summary>
            public const string Provider = Key + ":Provider";
            public const string ApiKey = Key + ":ApiKey";
            public const string Endpoint = Key + ":Endpoint";
            /// <summary>
            /// Comma-separated property editor aliases that the translator should process.
            /// Defaults to the well-known Umbraco text editors when empty.
            /// </summary>
            public const string AllowedEditors = Key + ":AllowedEditors";
        }
    }
}
