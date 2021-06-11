using System.IO;

namespace Canopy.Cli.Executable.Services
{
    public class GetPathWithSanitizedFolderName : IGetPathWithSanitizedFolderName
    {
        public string Execute(string path)
        {
            var folderName = Path.GetFileName(path);
            var parentPath = path.Substring(0, path.Length - folderName.Length);
            folderName = FileNameUtilities.Sanitize(folderName);
            return Path.Combine(parentPath, folderName);
        }
    }
}