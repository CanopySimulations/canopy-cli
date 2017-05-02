namespace Canopy.Cli.Executable
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;

    public static class PlatformUtilities
    {
        public static string AppDataFolder()
        {
            var userPath = Environment.GetEnvironmentVariable(
              RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
              ? "LOCALAPPDATA" 
              : "Home");

            var path = System.IO.Path.Combine(userPath, "canopy-cli");

            return path;
        }
    }
}
