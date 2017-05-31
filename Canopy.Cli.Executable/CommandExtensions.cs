using System;
using System.Linq;
using System.Text;
using Canopy.Api.Client;
using Microsoft.Extensions.CommandLineUtils;

namespace Canopy.Cli.Executable
{
    public static class CommandExtensions
    {
        public const string StandardErrorSuffix = " Use --help for more information.";

        public static void Required(this CommandOption option, string errorMessage)
        {
            switch(option.OptionType)
            {
                case CommandOptionType.SingleValue:
					if (string.IsNullOrWhiteSpace(option.Value()))
					{
						throw new RecoverableException(errorMessage + StandardErrorSuffix);
					}
					break;

                case CommandOptionType.MultipleValue:
                    if(option.Values.Count == 0 || option.Values.All(v => string.IsNullOrWhiteSpace(v)))
                    {
						throw new RecoverableException(errorMessage + StandardErrorSuffix);
					}
                    break;
            }
        }

		public static void Required(this CommandArgument argument, string errorMessage)
		{
            if (!argument.MultipleValues)
            {
                if (string.IsNullOrWhiteSpace(argument.Value))
                {
                    throw new RecoverableException(errorMessage + StandardErrorSuffix);
                }
            }
            else
            {
                if (argument.Values.Count == 0 || argument.Values.All(v => string.IsNullOrWhiteSpace(v)))
                {
                    throw new RecoverableException(errorMessage + StandardErrorSuffix);
                }
            }
		}

        public static string ValueOrPrompt(this CommandOption option, string promptMessage, string errorMessage, bool isSecret = false)
        {
            return ValueOrPrompt(option.Value(), promptMessage, errorMessage, isSecret);
        }

		public static string ValueOrPrompt(this CommandArgument argument, string promptMessage, string errorMessage, bool isSecret = false)
		{
            return ValueOrPrompt(argument.Value, promptMessage, errorMessage, isSecret);
		}

        private static string ValueOrPrompt(string input, string promptMessage, string errorMessage, bool isSecret)
        {
            var result = input;
            if (string.IsNullOrWhiteSpace(result))
            {
                Console.Write(promptMessage);
                if (isSecret)
                {
                    result = GetSecret();
                }
                else
                {
                    result = Console.ReadLine();
                }
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
