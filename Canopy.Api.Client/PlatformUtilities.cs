﻿namespace Canopy.Api.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
              : "HOME");

            if(string.IsNullOrWhiteSpace(userPath)){
                throw new RecoverableException("Could not find path for application data.");
            }

            var path = Path.Combine(userPath, "canopy-cli");

            try
            {
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
				}
		    }
            catch(Exception t)
            {
                throw new RecoverableException("Failed to create application folder at '" + path + "': " + t.Message);
            }

			return path;
        }
    }
}
