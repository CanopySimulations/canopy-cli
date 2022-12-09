using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class ReEncryptJobInputFiles : IReEncryptJobInputFiles
    {
        private readonly IFileOperations fileOperations;
        private readonly IReEncryptFile reEncryptFile;

        public ReEncryptJobInputFiles(IFileOperations fileOperations, IReEncryptFile reEncryptFile)
        {
            this.fileOperations = fileOperations;
            this.reEncryptFile = reEncryptFile;
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
                var newFileContents = await this.reEncryptFile.ExecuteAsync(fileContents, decryptingTenantShortName, studyDownloadMetadata, cancellationToken);
                
                await this.fileOperations.WriteAllTextAsync(filePath, newFileContents, cancellationToken);
                await this.fileOperations.WriteAllTextAsync(filePath + Constants.OrignalFileSuffix, fileContents, cancellationToken);
            }
        }
    }
}