using System;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Canopy.Cli.Executable.Services
{
    public class WaitForStudy : IWaitForStudy
    {
        private readonly IStudyClient studyClient;
        private readonly ILogger<WaitForStudy> logger;

        public WaitForStudy(
            IStudyClient studyClient,
            ILogger<WaitForStudy> logger)
        {
            this.logger = logger;
            this.studyClient = studyClient;
        }

        public async Task ExecuteAsync(string tenantId, string studyId, TimeSpan timeout)
        {
            this.logger.LogInformation("Waiting for study to complete.");
            var startTime = DateTime.UtcNow;
            while (true)
            {
                var now = DateTime.UtcNow;
                var elapsedTime = now - startTime;
                if (elapsedTime > timeout)
                {
                    throw new RecoverableException("Timed out waiting for study to complete.");
                }

                var studyMetadataResult = await this.studyClient.GetStudyMetadataAsync(tenantId, studyId);
                var studyData = studyMetadataResult.Study.Data as JObject;

                Guard.Operation(studyData != null, "Study data was not found or not in expected format.");

                var jobCount = studyData.Value<int>("jobCount");
                var completedJobCount = studyData.Value<int>("completedJobCount");
                if (jobCount > 0 && jobCount == completedJobCount)
                {
                    break;
                }

                await Task.Delay(this.GetSleepTimeDuration(elapsedTime));
            }

            this.logger.LogInformation("Study has completed.");
        }

        private TimeSpan GetSleepTimeDuration(TimeSpan elapsedTime)
        {
            if (elapsedTime.TotalSeconds < 30)
            {
                TimeSpan.FromSeconds(3);
            }
            else if (elapsedTime.TotalSeconds < 150)
            {
                return TimeSpan.FromSeconds(6);
            }
            else if (elapsedTime.TotalSeconds < 300)
            {
                return TimeSpan.FromSeconds(15);
            }

            return TimeSpan.FromSeconds(30);
        }
    }
}