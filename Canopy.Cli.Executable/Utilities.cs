using Canopy.Api.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Canopy.Cli.Executable
{
    public static class Utilities
    {
        private static readonly Serilog.Core.Logger Log = StandardLogging.CreateStandardSerilogConfiguration().CreateLogger();

        public const string ErrorLogFileName = "error.txt";

        public static void WriteTable(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> values)
        {
            var table = new ConsoleTables.ConsoleTable(headers.ToArray());

            foreach (var row in values)
            {
                table.AddRow(row.ToArray());
            }

            Log.Information(Environment.NewLine + table.ToString());
        }

        public static void HandleError(Exception t)
        {
            if (t is RecoverableException
                || t is HttpRequestException
                || t is CanopyApiException)
            {
                DisplayErrorMessage(t);
            }
            else
            {
                DisplayUnexpectedErrorMessage(t);
            }
        }

        private static void DisplayUnexpectedErrorMessage(Exception t)
        {
            Log.Error($"An error occurred. Run \"canopy last-error\" for more details.");
            WriteError(t);
        }

        private static void DisplayErrorMessage(Exception error)
        {
            Log.Error(error.Message);
            if (error.InnerException != null)
            {
                Log.Error(error.InnerException.Message);
            }

            if (error is CanopyApiException apiError)
            {
                Log.Error(apiError.Response);
            }

            WriteError(error);
        }

        private static void WriteError(Exception error)
        {
            try
            {
                var saveFolder = PlatformUtilities.AppDataFolder();
                var saveFile = Path.Combine(saveFolder, ErrorLogFileName);
                File.WriteAllText(saveFile, error.ToString());
            }
            catch (Exception t)
            {
                Log.Warning(t, "Failed to log error.");
                Log.Error(error, "Original error.");
            }
        }

        public static string? ReadError()
        {
            var saveFolder = PlatformUtilities.AppDataFolder();
            var saveFile = Path.Combine(saveFolder, ErrorLogFileName);
            if (!File.Exists(saveFile))
            {
                return null;
            }

            return File.ReadAllText(saveFile);
        }
    }
}
