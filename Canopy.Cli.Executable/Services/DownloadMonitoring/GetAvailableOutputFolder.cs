using System.IO;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class GetAvailableOutputFolder : IGetAvailableOutputFolder
    {
        private readonly IDirectoryExists directoryExists;
        
        public GetAvailableOutputFolder(IDirectoryExists directoryExists)
        {
            this.directoryExists = directoryExists;
        }

        public string Execute(string desiredOutputFolderPath)
        {
            var outputFolder = desiredOutputFolderPath;

            int suffix = 0;
            while (this.directoryExists.Execute(outputFolder))
            {
                suffix += 1;
                outputFolder = desiredOutputFolderPath + $" ({suffix})";
            }

            return outputFolder;
        }
    }
}