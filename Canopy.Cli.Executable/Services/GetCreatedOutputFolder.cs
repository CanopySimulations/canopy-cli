using System;
using System.IO;
using Canopy.Api.Client;

namespace Canopy.Cli.Executable.Services
{
    public class GetCreatedOutputFolder : IGetCreatedOutputFolder
    {
        public string Execute(DirectoryInfo folder)
        {
            return this.Execute(folder.FullName);
        }

        public string Execute(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception t)
            {
                throw new RecoverableException("Failed to create output folder.", t);
            }

            return path;
        }
    }

}