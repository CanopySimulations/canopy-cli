using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class ReEncryptJobInputFiles : IReEncryptJobInputFiles
    {
        private readonly IFileOperations fileOperations;
        private readonly IContainsEncryptedToken containsEncryptedToken;
        private readonly IReEncryptFile reEncryptFile;
        private readonly ILogger<ReEncryptJobInputFiles> logger;

        public ReEncryptJobInputFiles(
            IFileOperations fileOperations,
            IContainsEncryptedToken containsEncryptedToken,
            IReEncryptFile reEncryptFile,
            ILogger<ReEncryptJobInputFiles> logger)
        {
            this.fileOperations = fileOperations;
            this.containsEncryptedToken = containsEncryptedToken;
            this.reEncryptFile = reEncryptFile;
            this.logger = logger;
        }

        public async Task ExecuteAsync(
            string folder,
            string decryptingTenantShortName,
            StudyDownloadMetadata studyDownloadMetadata,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(decryptingTenantShortName))
            {
                return;
            }

            var studyBaselinePath = Path.Combine(folder, Constants.StudyBaselineFileName);
            var filePaths = this.fileOperations.GetFiles(folder, Constants.JobFileName, SearchOption.AllDirectories).ToList();
            filePaths.Add(studyBaselinePath);

            foreach (var filePath in filePaths)
            {
                if (!this.fileOperations.Exists(filePath))
                {
                    continue;
                }
                
                var fileContents = await this.fileOperations.ReadAllTextAsync(filePath, cancellationToken);
                if (!this.containsEncryptedToken.Execute(fileContents))
                {
                    continue;
                }

                this.logger.LogInformation($"Re-encrypting {filePath}...");
                var newFileContents = await this.reEncryptFile.ExecuteAsync(fileContents, decryptingTenantShortName, studyDownloadMetadata.SimVersion, cancellationToken);
                
                await this.fileOperations.WriteAllTextAsync(filePath, newFileContents, cancellationToken);
                await this.fileOperations.WriteAllTextAsync(filePath + Constants.OrignalFileSuffix, fileContents, cancellationToken);
            }
        }
    }
}