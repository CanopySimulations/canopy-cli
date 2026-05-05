using Canopy.Cli.Shared;
using Canopy.Cli.Shared.StudyProcessing.StudyScalars;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Commands
{
    public class TestPerformanceCommand : CanopyCommandBase
    {
        public record Parameters(FileInfo? ScalarResults, FileInfo? ScalarResultsMetadata);

        public override Command Create(IHost host)
        {
            var command = new Command("test-performance", "Tests the performance of certain operations.");

            var scalarResults = command.AddOption<FileInfo?>("--scalar-results", "-sr", null, "The path to the scalar results file.");
            var scalarResultsMetadata = command.AddOption<FileInfo?>("--scalar-results-metadata", "-srm", null, "The path to the scalar results metadata file.");

            command.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var parameters = new Parameters(
                    parseResult.GetValue(scalarResults),
                    parseResult.GetValue(scalarResultsMetadata));
                return host.Services.GetRequiredService<CommandRunner>().ExecuteAsync(parameters, cancellationToken);
            });

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

            public async Task ExecuteAsync(Parameters parameters, CancellationToken cancellationToken)
            {
                if (parameters.ScalarResults != null && parameters.ScalarResultsMetadata != null)
                {
                    await this.TestScalarResultsProcessing(parameters.ScalarResults, parameters.ScalarResultsMetadata, cancellationToken);
                }

            }

            private async Task TestScalarResultsProcessing(FileInfo scalarResults, FileInfo scalarResultsMetadata, CancellationToken cancellationToken)
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

                var scalarResultsContent = await File.ReadAllTextAsync(scalarResults.FullName, cancellationToken);
                var scalarResultsMetadataContent = await File.ReadAllTextAsync(scalarResultsMetadata.FullName, cancellationToken);

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
