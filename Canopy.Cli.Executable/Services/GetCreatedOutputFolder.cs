using System;
using System.IO;
using Canopy.Api.Client;

namespace Canopy.Cli.Executable.Services
{
    public class GetCreatedOutputFolder : IGetCreatedOutputFolder
    {
        private readonly IGetPathWithSanitizedFolderName getPathWithSanitizedFolderName;
        
        public GetCreatedOutputFolder(IGetPathWithSanitizedFolderName getPathWithSanitizedFolderName)
        {
            this.getPathWithSanitizedFolderName = getPathWithSanitizedFolderName;
        }

        public string Execute(DirectoryInfo folder)
        {
            return this.Execute(folder.FullName);
        }

        public string Execute(string path)
        {
            path = this.getPathWithSanitizedFolderName.Execute(path);

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