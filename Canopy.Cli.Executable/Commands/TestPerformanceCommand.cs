using System.Diagnostics;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Canopy.Cli.Shared;
using Canopy.Cli.Shared.StudyProcessing.StudyScalars;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Canopy.Cli.Executable.Commands
{
    public class TestPerformanceCommand : CanopyCommandBase
    {
        public record Parameters(FileInfo ScalarResults, FileInfo ScalarResultsMetadata);

        public override Command Create()
        {
            var command = new Command("test-performance", "Tests the performance of certain operations.");

            command.AddOption(new Option<FileInfo?>(
                new[] { "--scalar-results", "-sr" },
                description: "The path to the scalar results file.",
                getDefaultValue: () => null));

            command.AddOption(new Option<FileInfo?>(
                new[] { "--scalar-results-metadata", "-srm" },
                description: "The path to the scalar results metadata file.",
                getDefaultValue: () => null));

            command.Handler = CommandHandler.Create((IHost host, Parameters parameters) =>
                host.Services.GetRequiredService<CommandRunner>().ExecuteAsync(parameters));

            return command;
        }

        public class CommandRunner
        {
            private readonly ILogger<CommandRunner> logger;

            public CommandRunner(
                ILogger<CommandRunner> logger)
            {
                this.logger = logger;
            }

            public async Task ExecuteAsync(Parameters parameters)
            {
                if (parameters.ScalarResults != null && parameters.ScalarResultsMetadata != null)
                {
                    await this.TestScalarResultsProcessing(parameters.ScalarResults, parameters.ScalarResultsMetadata);
                }

            }

            private async Task TestScalarResultsProcessing(FileInfo scalarResults, FileInfo scalarResultsMetadata)
            {
                if (!scalarResults.Exists)
                {
                    this.logger.LogWarning("Scalar results file not found: {FilePath}", scalarResults.FullName);
                    return;
                }

                if (!scalarResultsMetadata.Exists)
                {
                    this.logger.LogWarning("Scalar results metadata file not found: {FilePath}", scalarResultsMetadata.FullName);
                    return;
                }

                var scalarResultsContent = File.ReadAllText(scalarResults.FullName);
                var scalarResultsMetadataContent = File.ReadAllText(scalarResultsMetadata.FullName);

                this.logger.LogInformation("ScalarResultsProcessing Started");
                
                var sw = Stopwatch.StartNew();
                var result = await WriteCombinedStudyScalarData.GetStudyScalarResultsFromMergedFiles(new MemoryFile(scalarResultsContent), new MemoryFile(scalarResultsMetadataContent));
                sw.Stop();

                this.logger.LogInformation("Parsed {ChannelCount} channels", result.Count);
                if (result.Count > 0)
                {
                    this.logger.LogInformation("First channel {Name} has {Count} points", result[0].Name, result[0].Data.Count);
                }

                this.logger.LogInformation("ScalarResultsProcessing: {Time}", sw.Elapsed);
            }
        }

        public class MemoryFile : IFile
        {
            private readonly string content;

            public MemoryFile(string content)
            {
                this.content = content;
            }

            public Task<byte[]> GetContentAsBytesAsync()
            {
                throw new NotImplementedException();
            }

            public Task<string> GetContentAsTextAsync()
            {
                return Task.FromResult(this.content);
            }

            public string FileName => throw new NotImplementedException();

            public string FullPath => throw new NotImplementedException();

            public string RelativePathToFile => throw new NotImplementedException();
        }
    }
}
