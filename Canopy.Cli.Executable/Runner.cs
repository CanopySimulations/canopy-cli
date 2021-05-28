using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.CommandLine.Parsing;
using Serilog;
using Canopy.Cli.Shared.StudyProcessing;
using Canopy.Cli.Executable.Services;
using Canopy.Api.Client;
using Canopy.Cli.Executable.Commands;

namespace Canopy.Cli.Executable
{
    public class Runner
    {
        public static async Task<int> ExecuteAsync(string[] args)
        {
            RootCommand? rootCommand = null;
            try
            {
                rootCommand = ConfigureCommands();
            }
            catch (Exception t)
            {
                throw new RecoverableException("Failed to configure commands.", t);
            }

            var parser = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseExceptionHandler((t, c) => Utilities.HandleError(t))
                .UseHost(Host.CreateDefaultBuilder, host =>
                {
                    host
                        .ConfigureHostConfiguration(ConfigureHostConfiguration)
                        .UseSerilog(
                            (context, loggerConfiguration) =>
                            {
                                StandardLogging.CreateStandardSerilogConfiguration(loggerConfiguration);
                            })
                        .ConfigureServices(ConfigureServices);
                })
                .Build();

            return await parser.InvokeAsync(args);
        }

        public static void ConfigureHostConfiguration(IConfigurationBuilder config)
        {
            config.AddUserSecrets<Program>(optional: true, reloadOnChange: true);
        }

        private static RootCommand ConfigureCommands()
        {
            var rootCommand = new RootCommand("Canopy CLI");

            var entryAssembly = Assembly.GetEntryAssembly();
            Guard.Operation(entryAssembly != null, "Entry assembly not found.");
            var commands = from t in entryAssembly.GetTypes()
                           where typeof(CanopyCommandBase).IsAssignableFrom(t) && t != typeof(CanopyCommandBase)
                           select (CanopyCommandBase?)Activator.CreateInstance(t);

            foreach (var command in commands.Where(v => v != null))
            {
                rootCommand.Add(command.Create());
            }

            return rootCommand;
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(context.Configuration);
            services.AddSingleton<IConnectionManager, ConnectionManager>();
            services.AddSingleton<IAuthenticationManager, AuthenticationManager>();
            services.AddSingleton<CanopyApiConfiguration>();

            AddApiClientServices(services);
            AddCommandRunnerServices(services);

            services.AddTransient<IEnsureConnected, EnsureConnected>();
            services.AddTransient<IEnsureAuthenticated, EnsureAuthenticated>();
            services.AddTransient<IProcessStudyResults, ProcessStudyResults>();
            services.AddTransient<IProcessLocalStudyResults, ProcessLocalStudyResults>();
            services.AddTransient<IGetUserIdFromUsername, GetUserIdFromUsername>();
            services.AddTransient<IGetStudy, GetStudy>();
            services.AddTransient<IGetSchemas, GetSchemas>();
            services.AddTransient<IGetConfigs, GetConfigs>();
            services.AddTransient<IMonitorDownloads, MonitorDownloads>();
            services.AddTransient<IRunDownloader, RunDownloader>();
            services.AddTransient<IProcessStudyResults, ProcessStudyResults>();
        }

        private static void AddCommandRunnerServices(IServiceCollection services)
        {
            var assembly = typeof(VersionCommand).Assembly;
            Guard.Operation(assembly != null, "Command assembly not found.");
            var apiClients = from t in assembly.GetTypes()
                             where t.Name == nameof(VersionCommand.CommandRunner)
                             select t;
            foreach (var client in apiClients)
            {
                services.AddTransient(client);
            }
        }

        private static void AddApiClientServices(IServiceCollection services)
        {
            var assembly = typeof(IAuthenticationManager).Assembly;
            Guard.Operation(assembly != null, "API Client assembly not found.");
            var apiClients = from t in assembly.GetTypes()
                             where typeof(CanopyApiClient).IsAssignableFrom(t) && t != typeof(CanopyApiClient)
                             select (Class: t, Interface: t.GetInterface($"I{t.Name}"));
            foreach (var client in apiClients)
            {
                services.AddTransient(client.Interface, client.Class);
            }
        }
    }
}
