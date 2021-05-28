using System.Globalization;
using System;
using System.Text;
using System.Threading;
using Canopy.Api.Client;

namespace Canopy.Cli.Executable
{
    public class CommandUtilities
    {
        public const string StandardErrorSuffix = " Use --help for more information.";

        public static CancellationTokenSource CreateCommandCancellationTokenSource()
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => cts.Cancel();
            return cts;
        }
        
        public static string ValueOrPrompt(string value, string promptMessage, string errorMessage, bool isSecret)
        {
            if(!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return Prompt(promptMessage, errorMessage, isSecret);
        }

        public static string Prompt(string promptMessage, string errorMessage, bool isSecret)
        {
            var result = string.Empty;

            Console.Write(promptMessage);
            if (isSecret)
            {
                result = GetSecret();
            }
            else
            {
                result = Console.ReadLine();
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new RecoverableException(errorMessage + StandardErrorSuffix);
            }

            return result;
        }
        
		private static string GetSecret()
		{
            var result = new StringBuilder();
			while (true)
			{
				ConsoleKeyInfo i = Console.ReadKey(true);
				if (i.Key == ConsoleKey.Enter)
				{
                    Console.WriteLine();
					break;
				}
				else if (i.Key == ConsoleKey.Backspace)
				{
					if (result.Length > 0)
					{
						result.Remove(result.Length - 1, 1);
						Console.Write("\b \b");
					}
				}
				else
				{
					result.Append(i.KeyChar);
					Console.Write("*");
				}
			}

			return result.ToString();
		}
    }
}