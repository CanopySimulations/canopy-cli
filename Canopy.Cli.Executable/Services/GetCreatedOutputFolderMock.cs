using System.IO;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.Services
{
    public class GetCreatedOutputFolderMock : IGetCreatedOutputFolder
    {
        private readonly ILogger<GetCreatedOutputFolderMock> logger;

        public GetCreatedOutputFolderMock(
            ILogger<GetCreatedOutputFolderMock> logger)
        {
            this.logger = logger;
        }

        public string Execute(DirectoryInfo folder)
        {
            return this.Execute(folder.FullName);
        }

        public string Execute(string path)
        {
            this.logger.LogInformation("Create directory requested: " + path);
            return path;
        }
    }

}