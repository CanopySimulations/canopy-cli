using System;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Canopy.Cli.Executable
{
    public class StandardLogging
    {
        public static LoggerConfiguration CreateStandardSerilogConfiguration(LoggerConfiguration? loggerConfiguration = null)
        {
            var result = loggerConfiguration ?? new LoggerConfiguration();
            return result
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    outputTemplate: "<{ThreadId}>[{Timestamp:HH:mm:ss} {Level}] {Message:lj}{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Literate);
        }

    }
}