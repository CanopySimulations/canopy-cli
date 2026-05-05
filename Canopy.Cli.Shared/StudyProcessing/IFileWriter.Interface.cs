using System;
using System.Collections.Generic;
using System.Threading;
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

        Task WriteExistingFile(IRootFolder root, IFile file, CancellationToken cancellationToken  = default) =>
            cancellationToken.IsCancellationRequested ? throw new OperationCanceledException() : WriteExistingFile(root, file);

        Task WriteExistingFile(IRootFolder root, IFile file);

        Task WriteNewFile(IRootFolder root, string relativePathToFile, string fileName, byte[] data, CancellationToken cancellationToken = default);

        Task WriteNewFile(IRootFolder root, string relativePathToFile, string fileName, byte[] data) =>
            WriteNewFile(root, relativePathToFile, fileName, data, default);

        Task WriteNewFile(IRootFolder root, string relativePathToFile, string fileName, IEnumerable<byte> data, CancellationToken cancellationToken = default) =>
            WriteNewFile(root, relativePathToFile, fileName, [.. data], cancellationToken);

        Task WriteNewFile(IRootFolder root, string relativePathToFile, string fileName, IEnumerable<byte> data) =>
            WriteNewFile(root, relativePathToFile, fileName, [.. data]);

        void ReportError(string message, Exception exception);

        Task DeleteProcessedFile(IRootFolder root, IFile file, CancellationToken cancellationToken = default)
            => cancellationToken.IsCancellationRequested ? throw new OperationCanceledException() : DeleteProcessedFile(root, file);

        Task DeleteProcessedFile(IRootFolder root, IFile file);
    }
}
