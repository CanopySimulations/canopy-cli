using System.Threading;
namespace Canopy.Cli.Executable.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Canopy.Cli.Shared;
    using Canopy.Cli.Shared.StudyProcessing;
    using Microsoft.Extensions.Logging;

    public class ProcessLocalStudyResults : IProcessLocalStudyResults
    {
        private readonly IProcessStudyResults processStudyResults;
        private readonly ILogger<ProcessLocalStudyResults> logger;

        public ProcessLocalStudyResults(
            IProcessStudyResults processStudyResults,
            ILogger<ProcessLocalStudyResults> logger)
        {
            this.logger = logger;
            this.processStudyResults = processStudyResults;
        }

        public async Task ExecuteAsync(string targetFolder, bool deleteProcessedFiles, CancellationToken cancellationToken)
        {
            foreach (var folder in Directory.EnumerateDirectories(targetFolder, "*", SearchOption.AllDirectories).Concat(new[] { targetFolder }))
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                
                try
                {
                    this.logger.LogInformation("Processing {0}", folder);
                    var root = new LocalFolder(folder);
                    var fileWriter = new FileWriter();
                    await this.processStudyResults.ExecuteAsync(
                        root,
                        fileWriter,
                        true,
                        deleteProcessedFiles,
                        1);
                }
                catch (Exception t)
                {
                    this.logger.LogError(t, "Failed to process folder: {0}", folder);
                }
            }
        }

        public class FileWriter : IFileWriter
        {
            public Task WriteExistingFile(IRootFolder root, IFile file)
            {
                return Task.CompletedTask;
            }

            public Task WriteNewFile(IRootFolder root, string relativePathToFile, string fileName, byte[] data)
            {
                LocalFolder.AssertRelativePathNotSupplied(relativePathToFile);
                File.WriteAllBytes(Path.Combine(((LocalFolder)root).FolderPath, fileName), data);
                return Task.CompletedTask;
            }

            public void ReportError(string message, Exception exception)
            {
                Console.WriteLine();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    Console.WriteLine(message);
                }

                if (exception != null)
                {
                    Console.WriteLine(exception);
                }
            }

            public Task DeleteProcessedFile(IRootFolder root, IFile file)
            {
                File.Delete(file.FullPath);
                return Task.CompletedTask;
            }
        }

        public class LocalFile : IFile
        {
            public LocalFile(string filePath)
            {
                this.FullPath = filePath;
                this.FileName = Path.GetFileName(filePath);
                this.RelativePathToFile = string.Empty;
            }

            public string FileName { get; }
            public string FullPath { get; }
            public string RelativePathToFile { get; }

            public Task<byte[]> GetContentAsBytesAsync()
            {
                return Task.FromResult(File.ReadAllBytes(this.FullPath));
            }

            public Task<string> GetContentAsTextAsync()
            {
                return Task.FromResult(File.ReadAllText(this.FullPath));
            }
        }

        public class LocalFolder : IRootFolder
        {
            public LocalFolder(string folderPath)
            {
                this.FolderPath = folderPath;
            }

            public string FolderPath { get; }

            public Task<IReadOnlyList<IFile>> GetFilesAsync()
            {
                var files = Directory.GetFiles(this.FolderPath, "*.*", SearchOption.TopDirectoryOnly);
                return Task.FromResult<IReadOnlyList<IFile>>(files.Select(v => new LocalFile(v)).ToList());
            }

            public Task<string> GetContentAsTextAsync(string relativePathToFile, string fileName)
            {
                AssertRelativePathNotSupplied(relativePathToFile);

                return Task.FromResult(File.ReadAllText(Path.Combine(this.FolderPath, fileName)));
            }

            public static void AssertRelativePathNotSupplied(string relativePathToFile)
            {
                if (!string.IsNullOrWhiteSpace(relativePathToFile))
                {
                    throw new Exception("Expected relative file path to be empty.");
                }
            }
        }
    }
}