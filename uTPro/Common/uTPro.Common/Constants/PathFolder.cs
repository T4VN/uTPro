namespace uTPro.Common.Constants
{
    public struct PathFolder
    {
        public static string DirectoryWWWRoot
        {
            get
            {
                return Path.Combine(PathFolder.DirectoryRootServer, "wwwroot");
            }
        }

        public static string DirectoryRootServer
        {
            get
            {
                return Environment.CurrentDirectory;
            }
        }

    }
}
