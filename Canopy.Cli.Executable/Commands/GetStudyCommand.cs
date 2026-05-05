using System.CommandLine;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Canopy.Cli.Shared;
using Canopy.Cli.Executable.Services.GetStudies;

namespace Canopy.Cli.Executable.Commands
{

    public class GetStudyCommand : CanopyCommandBase
    {
        public record Parameters(
            string OutputFolder,
            string TenantId,
            string StudyId,
            int? JobIndex,
            bool GenerateCsv,
            bool KeepBinary)
        {
            public static Parameters Random()
            {
                return new Parameters(
                    "./" + SingletonRandom.Instance.NextString(),
                    SingletonRandom.Instance.NextString(),
                    SingletonRandom.Instance.NextString(),
                    SingletonRandom.Instance.NextBoolean() ? SingletonRandom.Instance.NextInclusive(0, 1000) : null,
                    SingletonRandom.Instance.NextBoolean(),
                    SingletonRandom.Instance.NextBoolean());
            }
        };

        public override Command Create(IHost host)
        {
            var command = new Command("get-study", "Downloads the specified study or study job.");

            var outputFolder = command.AddOption("--output-folder", "-o", "./", "The output folder in which to save the files (defaults to the current directory).");
            var tenantId = command.AddOption("--tenant-id", "-t", string.Empty, "The tenancy from which download.");
            var studyId = command.AddOption("--study-id", "-s", string.Empty, "The study to download.");
            var jobIndex = command.AddOption<int?>("--job-index", "-j", null, "The job index download.");
            var generateCsv = command.AddOption("--generate-csv", "-csv", false, "Generate CSV files from binary files.");
            var keepBinary = command.AddOption("--keep-binary", "-bin", true, "Do not delete binary files which have been processed into CSV files (faster).");

            command.SetAction((ParseResult parseResult, CancellationToken cancellationToken) =>
            {
                var parameters = new Parameters(
                    parseResult.GetValue(outputFolder),
                    parseResult.GetValue(tenantId),
                    parseResult.GetValue(studyId),
                    parseResult.GetValue(jobIndex),
                    parseResult.GetValue(generateCsv),
                    parseResult.GetValue(keepBinary));
                return host.Services.GetRequiredService<IGetStudy>().ExecuteAndHandleCancellationAsync(
                    parameters with
                    {
                        StudyId = CommandUtilities.ValueOrPrompt(parameters.StudyId, "Study ID: ", "Study ID is required.", false),
                    },
                    cancellationToken);
            });

            return command;
        }
    }
}
