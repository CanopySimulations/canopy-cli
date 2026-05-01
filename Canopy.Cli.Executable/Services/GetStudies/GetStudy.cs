using Canopy.Cli.Executable.Commands;
using Humanizer;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public class GetStudy : IGetStudy
    {
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly IProcessLocalStudyResults processLocalStudyResults;
        private readonly IGetCreatedOutputFolder getCreatedOutputFolder;
        private readonly IDownloadStudy downloadStudy;

        private readonly ILogger<GetStudy> logger;

        public GetStudy(
            IEnsureAuthenticated ensureAuthenticated,
            IProcessLocalStudyResults processLocalStudyResults,
            IGetCreatedOutputFolder getCreatedOutputFolder,
            IDownloadStudy downloadStudy,
            ILogger<GetStudy> logger)
        {
            this.ensureAuthenticated = ensureAuthenticated;
            this.processLocalStudyResults = processLocalStudyResults;
            this.getCreatedOutputFolder = getCreatedOutputFolder;
            this.downloadStudy = downloadStudy;
            this.logger = logger;
        }

        public async Task ExecuteAndHandleCancellationAsync(GetStudyCommand.Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                await ExecuteWithResultAsync(parameters, cancellationToken);
            }
            catch (Exception t) when (ExceptionUtilities.IsFromCancellation(t))
            {
                // Just return if the task was cancelled.
            }
        }

        public async Task<GetStudyResult> ExecuteWithResultAsync(GetStudyCommand.Parameters parameters, CancellationToken cancellationToken)
        {
            var authenticatedUser = await this.ensureAuthenticated.ExecuteAsync();

            var outputFolder = this.getCreatedOutputFolder.Execute(parameters.OutputFolder);

            var tenantId = string.IsNullOrWhiteSpace(parameters.TenantId) ? authenticatedUser.TenantId : parameters.TenantId;
            var studyId = parameters.StudyId;
            var jobIndex = parameters.JobIndex;

            var getStudyQueryResult = await this.downloadStudy.ExecuteAsync(outputFolder, tenantId, studyId, jobIndex, cancellationToken);

            var generateCsvFiles = parameters.GenerateCsv;
            var generateBinaryFiles = this.ShouldGenerateBinaryFiles(outputFolder);

            var shouldProcessFiles = generateCsvFiles || generateBinaryFiles;

            if (shouldProcessFiles && !cancellationToken.IsCancellationRequested)
            {
                var deleteProcessedFiles = !parameters.KeepBinary;
                await this.processLocalStudyResults.ExecuteAsync(
                    outputFolder,
                    deleteProcessedFiles,
                    generateCsvFiles,
                    generateBinaryFiles,
                    cancellationToken);
            }

            return new GetStudyResult(getStudyQueryResult.Study.SimVersion);
        }

        private bool ShouldGenerateBinaryFiles(string? outputFolder)
        {
            if (outputFolder is null)
            {
                logger.LogWarning("Output folder is null, cannot check for binary files.");
                return false;
            }
            try
            {
                return !Directory.EnumerateFiles(outputFolder, "*.bin", SearchOption.AllDirectories)?.Any() ?? true;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error while checking for binary files in output folder: {OutputFolder}", outputFolder);
                return false;
            }
        }
    }
}