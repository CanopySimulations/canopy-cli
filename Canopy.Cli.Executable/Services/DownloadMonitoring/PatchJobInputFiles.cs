using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class PatchJobInputFiles : IPatchJobInputFiles
    {
        private readonly IPatchJobInputFile patchJobInputFile;
        private readonly IFileOperations fileOperations;

        public PatchJobInputFiles(IPatchJobInputFile patchJobInputFile, IFileOperations fileOperations)
        {
            this.patchJobInputFile = patchJobInputFile;
            this.fileOperations = fileOperations;
        }

        public async Task ExecuteAsync(
            string folder,
            CancellationToken cancellationToken)
        {
            var studyBaselinePath = Path.Combine(folder, Constants.StudyBaselineFileName);
            if (!this.fileOperations.Exists(studyBaselinePath))
            {
                return;
            }

            var studyBaselineText = await this.fileOperations.ReadAllTextAsync(studyBaselinePath, cancellationToken);
            foreach (var file in this.fileOperations.GetFiles(folder, Constants.JobPatchFileName, SearchOption.AllDirectories))
            {
                await this.patchJobInputFile.ExecuteAsync(studyBaselineText, file, cancellationToken);
            }
        }
    }
}