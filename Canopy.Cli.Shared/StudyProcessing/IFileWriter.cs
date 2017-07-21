using System;
using System.Threading.Tasks;

namespace Canopy.Cli.Shared
{
        public interface IFileWriter
        {
            Task WriteExistingFile(IRootFolder root, IFile file);

            Task WriteNewFile(IRootFolder root, string relativePathToFile, string fileName, byte[] data);

            void ReportError(string message, Exception exception);

            Task DeleteProcessedFile(IRootFolder root, IFile file);
        }
}
