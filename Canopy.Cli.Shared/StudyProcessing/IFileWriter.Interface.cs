using System;
using System.Threading.Tasks;
using Canopy.Cli.Shared.StudyProcessing;

namespace Canopy.Cli.Shared
{
    public interface IFileWriter
    {
        /// <summary>
        /// Determines if this writer will write the specified type of generated file.
        /// </summary>
        /// <param name="fileType">The type of file to check.</param>
        /// <returns>True if this writer will write files of the specified type.</returns>
        bool Writes(ResultsFile fileType);

        Task WriteExistingFile(IRootFolder root, IFile file);

        Task WriteNewFile(IRootFolder root, string relativePathToFile, string fileName, byte[] data);

        void ReportError(string message, Exception exception);

        Task DeleteProcessedFile(IRootFolder root, IFile file);
    }
}
