using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Commands
{
    public class HashCommand : CanopyCommandBase
    {
        public record Parameters(string Input);

        public override Command Create()
        {
            var command = new Command("hash", "Hashes a given string. Defaults to SHA256.");

            command.AddArgument(new Argument<string>(
                "input",
                description: "The input string to hash.",
                getDefaultValue: () => string.Empty));

            command.Handler = CommandHandler.Create((IHost host, Parameters parameters) =>
                host.Services.GetRequiredService<CommandRunner>().ExecuteAsync(
                    parameters with
                    {
                        Input = CommandUtilities.ValueOrPrompt(parameters.Input, "Input string: ", "Input string is required.", false),
                    }));

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

            public Task ExecuteAsync(Parameters parameters)
            {
                this.logger.LogInformation(GetHash(parameters.Input));
                return Task.CompletedTask;
            }

            public static string GetHash(string input)
            {
                var hashAlgorithm = SHA256.Create();
                byte[] byteValue = System.Text.Encoding.UTF8.GetBytes(input);
                byte[] byteHash = hashAlgorithm.ComputeHash(byteValue);
                return Convert.ToBase64String(byteHash);
            }
        }
    }
}
