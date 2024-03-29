﻿using System.CommandLine;
using Microsoft.Extensions.Hosting;
using System.CommandLine.Invocation;
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

        public override Command Create()
        {
            var command = new Command("get-study", "Downloads the specified study or study job.");

            command.AddOption(new Option<string>(
                new[] { "--output-folder", "-o" },
                description: $"The output folder in which to save the files (defaults to the current directory).",
                getDefaultValue: () => "./"));

            command.AddOption(new Option<string>(
                new[] { "--tenant-id", "-t" },
                description: $"The tenancy from which download.",
                getDefaultValue: () => string.Empty));

            command.AddOption(new Option<string>(
                new[] { "--study-id", "-s" },
                description: $"The study to download.",
                getDefaultValue: () => string.Empty));

            command.AddOption(new Option<int?>(
                new[] { "--job-index", "-j" },
                description: $"The job index download.",
                getDefaultValue: () => null));

            command.AddOption(new Option<bool>(
                new[] { "--generate-csv", "-csv" },
                description: $"Generate CSV files from binary files.",
                getDefaultValue: () => false));

            command.AddOption(new Option<bool>(
                new[] { "--keep-binary", "-bin" },
                description: $"Do not delete binary files which have been processed into CSV files (faster).",
                getDefaultValue: () => true));

            command.Handler = CommandHandler.Create((IHost host, Parameters parameters) =>
            {
                var cts = CommandUtilities.CreateCommandCancellationTokenSource();
                host.Services.GetRequiredService<IGetStudy>().ExecuteAndHandleCancellationAsync(
                    parameters with
                    {
                        StudyId = CommandUtilities.ValueOrPrompt(parameters.StudyId, "Study ID: ", "Study ID is required.", false),
                    },
                    cts.Token);
            });

            return command;
        }
    }
}
