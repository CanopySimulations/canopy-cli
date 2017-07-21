using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Canopy.Cli.Shared;
using Canopy.Cli.Shared.StudyProcessing;
using Microsoft.Extensions.CommandLineUtils;

namespace Canopy.Cli.Executable.Commands
{
    public class ProcessStudyResultsCommand : CanopyCommandBase
    {
        private readonly ProcessStudyResults processStudyResults = new ProcessStudyResults();

        private readonly CommandOption targetOption;
        private readonly CommandOption keepOriginalFilesOption;

        public ProcessStudyResultsCommand()
        {
            this.Name = "process-study-results";
            this.Description = "Creates user friendly files from raw study results.";

            this.targetOption = this.Option(
                "-t | --target",
                $"The folder to process. The current directory is used if omitted.",
                CommandOptionType.SingleValue);

            this.keepOriginalFilesOption = this.Option(
                "-ko | --keep-original",
                $"Do not delete files which have been processed.",
                CommandOptionType.NoValue);
        }

        protected override async Task<int> ExecuteAsync()
        {
            var deleteProcessedFiles = !this.keepOriginalFilesOption.HasValue();
            var targetFolder = this.targetOption.Value() ?? Directory.GetCurrentDirectory();

            if (!Directory.Exists(targetFolder))
            {
                Console.WriteLine();
                Console.WriteLine("Folder not found: " + targetFolder);
                return 1;
            }

            foreach (var folder in Directory.EnumerateDirectories(targetFolder, "*", SearchOption.AllDirectories).Concat(new[] { targetFolder }))
            {
                try
                {
                    Console.WriteLine();
                    Console.WriteLine("Processing: " + folder);
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
                    Console.WriteLine();
                    Console.WriteLine("Failed to process folder: " + folder);
                    Console.WriteLine(t);
                }
            }

            return 0;
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
                File.WriteAllBytes(Path.Combine(((LocalFolder) root).FolderPath, fileName), data);
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
                var files = Directory.GetFiles(this.FolderPath, "*.bin", SearchOption.TopDirectoryOnly);
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