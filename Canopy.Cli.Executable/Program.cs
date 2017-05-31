using Canopy.Api.Client;
using System;
using System.Text;
using Microsoft.Extensions.CommandLineUtils;

namespace Canopy.Cli.Executable
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
				// NOTE: This application uses the .NET core command line parser.
				// Article: https://msdn.microsoft.com/en-us/magazine/mt763239.aspx
				// Better Article: https://gist.github.com/iamarcel/8047384bfbe9941e52817cf14a79dc34
				// Example usage: https://github.com/ronnieoverby/AtomicUtils/tree/master/src/AtomicUtils
				
                var runner = new Runner();
                return runner.Execute(args);
            }
            catch (Exception t)
            {
                Console.WriteLine();
                Console.WriteLine(t);
                return 1;
            }
        }
    }
}