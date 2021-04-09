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

                // https://github.com/Azure/azure-storage-net-data-movement#increase-net-http-connections-limit
                System.Net.ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount * 16;
                System.Net.ServicePointManager.Expect100Continue = false;

                var runner = new Runner();
                return runner.Execute(args);
            }
            catch (Exception t)
            {
                Utilities.HandleError(t);
                return 1;
            }
        }
    }
}