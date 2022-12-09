using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class PatchJobInputFile : IPatchJobInputFile
    {
        private readonly IFileOperations fileOperations;

        public PatchJobInputFile(IFileOperations fileOperations)
        {
            this.fileOperations = fileOperations;
        }

        public async Task ExecuteAsync(
            string studyBaselineText,
            string patchFilePath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(patchFilePath))
            {
                return;
            }

            var parentFolderPath = Path.GetDirectoryName(patchFilePath);
            if (parentFolderPath == null)
            {
                throw new InvalidOperationException("Failed to get parent directory name.");
            }

            var jobFilePath = Path.Combine(parentFolderPath, Constants.JobFileName);
            if (this.fileOperations.Exists(jobFilePath))
            {
                return;
            }

            var patch = await this.fileOperations.ReadAllTextAsync(patchFilePath, cancellationToken);

            var dmp = DiffMatchPatch.DiffMatchPatchModule.Default;
            var patches = dmp.PatchFromText(patch);
            var result = dmp.PatchApply(patches, studyBaselineText);

            var jobFileText = result?.FirstOrDefault()?.ToString();
            if (jobFileText == null)
            {
                return;
            }

            await this.fileOperations.WriteAllTextAsync(jobFilePath, jobFileText, cancellationToken);
        }
    }
}