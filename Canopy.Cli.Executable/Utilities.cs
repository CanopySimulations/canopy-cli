using Canopy.Api.Client;
using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Canopy.Cli.Executable
{
    public static class Utilities
    {
        public const string ErrorLogFileName = "error.txt";

        public static string GetCreatedOutputFolder(CommandOption option)
        {
            var outputFolder = option.Value();
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                outputFolder = "./";
            }
            else
            {
                try
                {
                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                    }
                }
                catch (Exception t)
                {
                    throw new RecoverableException("Failed to create output folder.", t);
                }
            }

            return outputFolder;
        }

        public static void WriteTable(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> values, int padding = 1)
        {
            var lines = new List<List<string>>
            {
                headers.ToList(),
                headers.Select(v => string.Concat(Enumerable.Repeat("-", v?.Length ?? 0))).ToList()
            }
            .Concat(values.Select(v => v.ToList())).ToList();

            // Calculate maximum numbers for each element accross all lines
            var numElements = lines[0].Count;
            var maxValues = new int[numElements];
            for (int i = 0; i < numElements; i++)
            {
                maxValues[i] = lines.Max(x => (x.Count > i + 1 && x[i] != null ? x[i].Length : 0)) + padding;
            }

            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                sb.AppendLine();
                sb.Append(" ");
                for (int i = 0; i < line.Count; i++)
                {
                    var value = line[i];
                    // Append the value with padding of the maximum length of any value for this element
                    if (value != null)
                    {
                        sb.Append(value.PadRight(maxValues[i]));
                    }
                }
            }

            Console.WriteLine(sb.ToString());
        }

        public static void HandleError(Exception t)
        {
            if (t is RecoverableException
                || t is HttpRequestException
                || t is CommandParsingException
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
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine($"An error occurred. Run \"canopy last-error\" for more details.");
            Console.ResetColor();
            WriteError(t);
        }

        private static void DisplayErrorMessage(Exception error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine();
            Console.WriteLine(error.Message);
            if (error.InnerException != null)
            {
                Console.WriteLine(error.InnerException.Message);
            }

            if (error is CanopyApiException apiError)
            {
                Console.WriteLine(apiError.Response);
            }

            Console.ResetColor();
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
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine();
                Console.WriteLine("Failed to log error:");
                Console.WriteLine(t);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine();
                Console.WriteLine("Original error:");
                Console.WriteLine(error);
                Console.ResetColor();
            }
        }

        public static string ReadError()
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
