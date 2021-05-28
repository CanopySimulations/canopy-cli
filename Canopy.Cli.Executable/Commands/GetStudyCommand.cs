﻿using Canopy.Api.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;
using System.Linq;
using System.Text.RegularExpressions;
using Canopy.Cli.Executable.Services;
using System.CommandLine;
using System.IO;
using Microsoft.Extensions.Hosting;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;

namespace Canopy.Cli.Executable.Commands
{

    public class GetStudyCommand : CanopyCommandBase
    {
        public record Parameters(
            DirectoryInfo OutputFolder,
            string StudyId,
            bool GenerateCsv,
            bool KeepBinary);

        public override Command Create()
        {
            var command = new Command("get-study", "Downloads the specified study or study job.");

            command.AddOption(new Option<DirectoryInfo>(
                new [] { "--output-folder", "-o" },
                description: $"The output folder in which to save the files (defaults to the current directory).",
                getDefaultValue: () => new DirectoryInfo("./")));

            command.AddOption(new Option<string>(
                new [] { "--study-id", "-s" },
                description: $"The study to download.",
                getDefaultValue: () => string.Empty));

            command.AddOption(new Option<bool>(
                new [] { "--generate-csv", "-csv" },
                description: $"Generate CSV files from binary files.",
                getDefaultValue: () => false));

            command.AddOption(new Option<bool>(
                new [] { "--keep-binary", "-bin" },
                description: $"Do not delete binary files which have been processed into CSV files (faster).",
                getDefaultValue: () => true));

            command.Handler = CommandHandler.Create((IHost host, Parameters parameters) =>
                host.Services.GetRequiredService<IGetStudy>().ExecuteAsync(
                    parameters with
                    {
                        StudyId = CommandUtilities.ValueOrPrompt(parameters.StudyId, "Study ID: ", "Study ID is required.", false),
                    }));

            return command;
        }

    }
}
