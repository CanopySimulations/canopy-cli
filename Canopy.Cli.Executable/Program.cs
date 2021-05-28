using System;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.DataMovement;
using Serilog;

namespace Canopy.Cli.Executable
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // https://github.com/Azure/azure-storage-net-data-movement#increase-net-http-connections-limit
            System.Net.ServicePointManager.DefaultConnectionLimit = Environment.ProcessorCount * 16;
            System.Net.ServicePointManager.Expect100Continue = false;

            TransferManager.Configurations.ParallelOperations = System.Net.ServicePointManager.DefaultConnectionLimit;
            TransferManager.Configurations.MaxListingConcurrency = 20;
            TransferManager.Configurations.BlockSize = 20971520;

            Log.Logger = StandardLogging.CreateStandardSerilogConfiguration().CreateLogger();

            int result;
            try
            {
                result = await Runner.ExecuteAsync(args);
            }
            catch (Exception t)
            {
                Utilities.HandleError(t);
                result = 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }

            return result;
        }
    }
}