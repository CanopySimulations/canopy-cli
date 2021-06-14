using System;
using System.Threading.Tasks;
using Canopy.Cli.Executable.Commands;
using System.Threading;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public class GetStudy : IGetStudy
    {
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly IProcessLocalStudyResults processLocalStudyResults;
        private readonly IGetCreatedOutputFolder getCreatedOutputFolder;
        private readonly IDownloadStudy downloadStudy;

        public GetStudy(
            IEnsureAuthenticated ensureAuthenticated,
            IProcessLocalStudyResults processLocalStudyResults,
            IGetCreatedOutputFolder getCreatedOutputFolder,
            IDownloadStudy downloadStudy)
        {
            this.ensureAuthenticated = ensureAuthenticated;
            this.processLocalStudyResults = processLocalStudyResults;
            this.getCreatedOutputFolder = getCreatedOutputFolder;
            this.downloadStudy = downloadStudy;
        }

        public async Task ExecuteAsync(GetStudyCommand.Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var authenticatedUser = await this.ensureAuthenticated.ExecuteAsync();

                var outputFolder = this.getCreatedOutputFolder.Execute(parameters.OutputFolder);

                var tenantId = string.IsNullOrWhiteSpace(parameters.TenantId) ? authenticatedUser.TenantId : parameters.TenantId;
                var studyId = parameters.StudyId;
                var jobIndex = parameters.JobIndex;

                await this.downloadStudy.ExecuteAsync(outputFolder, tenantId, studyId, jobIndex, cancellationToken);

                var generateCsvFiles = parameters.GenerateCsv;

                if (generateCsvFiles && !cancellationToken.IsCancellationRequested)
                {
                    var deleteProcessedFiles = !parameters.KeepBinary;
                    await this.processLocalStudyResults.ExecuteAsync(outputFolder, deleteProcessedFiles, cancellationToken);
                }
            }
            catch (Exception t) when (ExceptionUtilities.IsFromCancellation(t))
            {
                // Just return if the task was cancelled.
            }
        }
    }
}