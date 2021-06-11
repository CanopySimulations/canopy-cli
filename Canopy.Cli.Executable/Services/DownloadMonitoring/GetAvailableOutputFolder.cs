using System.IO;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class GetAvailableOutputFolder : IGetAvailableOutputFolder
    {
        private readonly IGetPathWithSanitizedFolderName getPathWithSanitizedFolderName;
        private readonly IDirectoryExists directoryExists;

        public GetAvailableOutputFolder(
            IGetPathWithSanitizedFolderName getPathWithSanitizedFolderName,
            IDirectoryExists directoryExists)
        {
            this.getPathWithSanitizedFolderName = getPathWithSanitizedFolderName;
            this.directoryExists = directoryExists;
        }

        public string Execute(string desiredOutputFolderPath)
        {
            desiredOutputFolderPath = this.getPathWithSanitizedFolderName.Execute(desiredOutputFolderPath);
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