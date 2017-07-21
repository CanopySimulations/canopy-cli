using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Microsoft.Extensions.CommandLineUtils;

namespace Canopy.Cli.Executable.Commands
{
    public class HashCommand : CanopyCommandBase
    {
		private readonly CommandArgument inputArgument;
		
        public HashCommand()
        {
            this.RequiresConnection = false;
            this.RequiresAuthentication = false;

            this.Name = "hash";
            this.Description = "Hashes a given string. Defaults to SHA256.";

            this.inputArgument = this.Argument("<input>", "The input string to hash.");
        }

        protected override Task<int> ExecuteAsync()
        {
            var input = this.inputArgument.ValueOrPrompt("Input string: ", "Input string is required.");

            Console.WriteLine(GetHash(input));
            return Task.FromResult(0);
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
