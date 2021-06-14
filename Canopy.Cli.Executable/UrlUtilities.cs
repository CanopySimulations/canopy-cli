namespace Canopy.Cli.Executable
{
    public static class UrlUtilities
    {
        public const char UrlDirectorySeparator = '/';

        public static string AppendFolderToUrl(string url, string folder)
        {
            if (url.EndsWith(UrlDirectorySeparator))
            {
                return url + folder + UrlDirectorySeparator;
            }

            return url + UrlDirectorySeparator + folder + UrlDirectorySeparator;
        }
    }
}