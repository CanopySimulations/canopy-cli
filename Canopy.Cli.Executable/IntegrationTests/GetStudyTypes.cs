using System.Linq;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Canopy.Cli.Executable.Services;
using Microsoft.Extensions.Logging;

namespace Canopy.Cli.Executable.IntegrationTests
{
    public class GetStudyTypes
    {
        private readonly ILogger<GetStudyTypes> logger;
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly IStudyClient studyClient;

        public GetStudyTypes(
            IEnsureAuthenticated ensureAuthenticated,
            IStudyClient studyClient,
            ILogger<GetStudyTypes> logger)
        {
            this.studyClient = studyClient;
            this.ensureAuthenticated = ensureAuthenticated;
            this.logger = logger;
        }

        public async Task _01_GetStudyTypes()
        {
            var authenticationUser = await this.ensureAuthenticated.ExecuteAsync();
            
            var result = await this.studyClient.GetStudyTypesAsync(authenticationUser.TenantId);

            this.logger.LogInformation("Found {Count} Study Types: {List}", result.StudyTypes.Count, string.Join(", ", result.StudyTypes.Select(v => v.StudyType)));
            this.logger.LogInformation("Found {Count} Sim Types: {List}", result.SimTypes.Count, string.Join(", ", result.SimTypes.Select(v => v.SimType)));
            this.logger.LogInformation("Found {Count} Config Types: {List}", result.ConfigTypes.Count, string.Join(", ", result.ConfigTypeMetadata.Select(v => v.SingularKey)));
        }
    }
}