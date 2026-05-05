using System;
using System.Threading.Tasks;
using Serilog;

namespace Canopy.Cli.Executable
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
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