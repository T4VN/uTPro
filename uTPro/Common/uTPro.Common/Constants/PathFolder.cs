namespace uTPro.Common.Constants
{
    public struct PathFolder
    {
        /// <summary>
        /// Actual web root (wwwroot) path. Set once at startup from
        /// <c>IWebHostEnvironment.WebRootPath</c> so it respects the configured web root
        /// (e.g. uTPro:Hosting:RootPath). When not initialised (design-time, tooling,
        /// tests) it falls back to <c>&lt;ContentRoot&gt;\wwwroot</c>.
        /// </summary>
        public static string? WebRootPathOverride { get; set; }

        /// <summary>
        /// Actual content root path. Set once at startup from
        /// <c>IWebHostEnvironment.ContentRootPath</c>. Falls back to the process current
        /// directory when not initialised.
        /// </summary>
        public static string? ContentRootPathOverride { get; set; }

        public static string DirectoryWWWRoot
        {
            get
            {
                return !string.IsNullOrEmpty(WebRootPathOverride)
                    ? WebRootPathOverride
                    : Path.Combine(DirectoryRootServer, "wwwroot");
            }
        }

        public static string DirectoryRootServer
        {
            get
            {
                return !string.IsNullOrEmpty(ContentRootPathOverride)
                    ? ContentRootPathOverride
                    : Environment.CurrentDirectory;
            }
        }

    }
}
