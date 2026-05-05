using System;
using System.CommandLine;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Commands
{
    public class HashCommand : CanopyCommandBase
    {
        public record Parameters(string Input);

        public override Command Create(IHost host)
        {
            var command = new Command("hash", "Hashes a given string. Defaults to SHA256.");

            var input = command.AddArgument("input", string.Empty, "The input string to hash.");

            command.SetAction((ParseResult parseResult) =>
            {
                var parameters = new Parameters(
                    parseResult.GetValue(input));
                host.Services.GetRequiredService<CommandRunner>().Execute(
                    parameters with
                    {
                        Input = CommandUtilities.ValueOrPrompt(parameters.Input, "Input string: ", "Input string is required.", false),
                    });
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

            public void Execute(Parameters parameters)
            {
                this.logger.LogInformation(GetHash(parameters.Input));
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
