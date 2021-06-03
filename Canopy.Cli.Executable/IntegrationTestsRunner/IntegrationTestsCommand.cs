using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.IntegrationTestsRunner
{
    public class IntegrationTestsCommand : CanopyCommandBase
    {
        public const string IntegrationTestsNamespaceSuffix = ".IntegrationTests";
        public const string Name = "integration-tests";

        const string DefaultCredentialsEnvironmentVariableKey = "CANOPY_PYTHON_INTEGRATION_TEST_CREDENTIALS";

        public record Parameters(
            string Name
        );
            // string Endpoint,
            // string ClientId,
            // string ClientSecret,
            // string Username,
            // string Company,
            // string Password,
            // string CredentialsKey);

        public override Command Create()
        {
            var command = new Command(Name, "Runs the CLI integration tests.");

            // command.AddOption(new Option<string>(
            //     new[] { "--endpoint", "-e" },
            //     description: $"The API endpoint to connect to (defaults to {ConnectionManager.DefaultApiEndpoint}).",
            //     getDefaultValue: () => ConnectionManager.DefaultApiEndpoint));

            // command.AddOption(new Option<string>(
            //     new[] { "--client-id", "-cid" },
            //     description: $"Your client ID, as provided by Canopy Simulations.",
            //     getDefaultValue: () => string.Empty));

            // command.AddOption(new Option<string>(
            //     new[] { "--client-secret", "-s" },
            //     description: $"Your client secret, as provided by Canopy Simulations.",
            //     getDefaultValue: () => string.Empty));

            // command.AddOption(new Option<string>(
            //     new[] { "--username", "-u" },
            //     description: "Your username.",
            //     getDefaultValue: () => string.Empty));

            // command.AddOption(new Option<string>(
            //     new[] { "--company", "-c" },
            //     description: "Your company.",
            //     getDefaultValue: () => string.Empty));

            // command.AddOption(new Option<string>(
            //     new[] { "--password", "-p" },
            //     description: "Your password.",
            //     getDefaultValue: () => string.Empty));

            // command.AddOption(new Option<string>(
            //     new[] { "--credentials-key", "-k" },
            //     description: "The environment variable key that contains your credentials (see the canopy-python integration tests for the format).",
            //     getDefaultValue: () => string.Empty));

            command.AddOption(new Option<string>(
                new[] { "--name", "-n" },
                description: "The name of the integration test to run.",
                getDefaultValue: () => string.Empty));

            command.Handler = CommandHandler.Create((IHost host, Parameters parameters) =>
                host.Services.GetRequiredService<CommandRunner>().ExecuteAsync(host, parameters));

            return command;
        }

        public class CommandRunner
        {
            private readonly ILogger<CommandRunner> logger;
            // private readonly IGetCredentialsFromEnvironmentVariable getCredentialsFromEnvironmentVariable;

            public CommandRunner(
                // IGetCredentialsFromEnvironmentVariable getCredentialsFromEnvironmentVariable,
                ILogger<CommandRunner> logger)
            {
                // this.getCredentialsFromEnvironmentVariable = getCredentialsFromEnvironmentVariable;
                this.logger = logger;
            }

            public async Task ExecuteAsync(IHost host, Parameters parameters)
            {
                // if (!string.IsNullOrWhiteSpace(parameters.CredentialsKey))
                // {
                //     parameters = this.getCredentialsFromEnvironmentVariable.Execute(parameters.CredentialsKey);
                // }
                // else
                // {
                //     parameters = parameters with
                //     {
                //         ClientId = CommandUtilities.ValueOrPrompt(parameters.ClientId, "Client ID: ", "Client ID is required.", false),
                //         ClientSecret = CommandUtilities.ValueOrPrompt(parameters.ClientSecret, "Client Secret: ", "Client Secret is required.", true),
                //         Username = CommandUtilities.ValueOrPrompt(parameters.Username, "Username: ", "Username is required.", false),
                //         Company = CommandUtilities.ValueOrPrompt(parameters.Company, "Company: ", "Company is required.", false),
                //         Password = CommandUtilities.ValueOrPrompt(parameters.Password, "Password: ", "Password is required.", true),
                //     };
                // }

                var integrationTests = GetIntegrationTestClasses(parameters.Name);

                this.logger.LogInformation("Found {0} integration test classes.", integrationTests.Count);
                Guard.Operation(integrationTests.Count > 0, "No integration tests found.");

                int failureCount = 0;
                foreach (var integrationTest in integrationTests)
                {
                    this.logger.LogInformation("Instantiating {0}", integrationTest.Name);
                    var instance = host.Services.GetRequiredService(integrationTest);

                    var methods = GetIntegrationTestMethods(integrationTest);
                    this.logger.LogInformation("Found {0} methods.", methods.Count);

                    foreach (var method in methods)
                    {
                        try
                        {
                            this.logger.LogInformation("Running {0}.{1}...", integrationTest.Name, method.Name);
                            var result = method.Invoke(instance, null);
                            if (result is Task task)
                            {
                                await task;
                            }
                            this.logger.LogInformation("{0}.{1} passed.", integrationTest.Name, method.Name);
                        }
                        catch (Exception t)
                        {
                            this.logger.LogError(t, "Integration test {0}.{1} failed.", integrationTest.Name, method.Name);
                            failureCount += 1;
                            break;
                        }
                    }
                }

                if (failureCount > 0)
                {
                    throw new RecoverableException($"{failureCount} integration test(s) failed.");
                }
            }

            private static List<MethodInfo> GetIntegrationTestMethods(System.Type integrationTest)
            {
                return integrationTest
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance).OrderBy(v => v.Name)
                    .Where(m => m.DeclaringType != typeof(object))
                    .ToList();
            }

            public static List<System.Type> GetIntegrationTestClasses(string name)
            {
                var assembly = typeof(IntegrationTestsCommand).Assembly;
                Guard.Operation(assembly != null, "Integration tests assembly not found.");
                var integrationTests = (from t in assembly.GetTypes()
                                        where t.IsClass
                                            && t.Namespace != null
                                            && t.Namespace.EndsWith(IntegrationTestsNamespaceSuffix)
                                            && !t.IsNested
                                            && (string.IsNullOrWhiteSpace(name) || t.Name == name)
                                        orderby t.Name
                                        select t).ToList();
                return integrationTests;
            }
        }
    }
}
