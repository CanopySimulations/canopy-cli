using System.Collections.Generic;
using System.Threading.Tasks;

namespace Canopy.Cli.Shared
{
        public interface IRootFolder
        {
            Task<IReadOnlyList<IFile>> GetFilesAsync();

            Task<string> GetContentAsTextAsync(string relativePathToFile, string fileName);
        }
}
