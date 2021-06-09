using System.Collections.Generic;
using System.Threading.Tasks;
using Canopy.Api.Client;
using Canopy.Cli.Shared;
using NSubstitute;
using Xunit;

namespace Canopy.Cli.Executable.Services
{
    public class StudyTypesCacheTests
    {
        private static string TenantId1 = SingletonRandom.Instance.NextString();
        private static string TenantId2 = SingletonRandom.Instance.NextString();

        private readonly IStudyClient studyClient;

        private readonly StudyTypesCache target;

        public StudyTypesCacheTests()
        {
            this.studyClient = Substitute.For<IStudyClient>();

            this.target = new StudyTypesCache(this.studyClient);
        }

        [Fact]
        public async Task ItShouldOnlyRequestStudyTypesOncePerTenant()
        {
            var tenant1StudyTypes = this.CreateStudyTypesQueryResult("straightSim");
            var tenant2StudyTypes = this.CreateStudyTypesQueryResult("apexSim");
            
            this.studyClient.GetStudyTypesAsync(TenantId1)
                .Returns(Task.FromResult(tenant1StudyTypes));
            this.studyClient.GetStudyTypesAsync(TenantId2)
                .Returns(Task.FromResult(tenant2StudyTypes));

            var result1 = await this.target.Get(TenantId1);
            var result2 = await this.target.Get(TenantId2);
            var result3 = await this.target.Get(TenantId1);

            Assert.Same(tenant1StudyTypes, result1);
            Assert.Same(tenant2StudyTypes, result2);
            Assert.Same(tenant1StudyTypes, result3);

            var configMetadataResult1 = await this.target.GetConfigTypeMetadata(TenantId1, "straightSim");
            await Assert.ThrowsAsync<RecoverableException>(() => this.target.GetConfigTypeMetadata(TenantId2, "straightSim"));

            Assert.Equal("straightSim", configMetadataResult1.SingularKey);

            await this.studyClient.Received(1).GetStudyTypesAsync(TenantId1);
            await this.studyClient.Received(1).GetStudyTypesAsync(TenantId2);
        }

        private GetStudyTypesQueryResult CreateStudyTypesQueryResult(string configType)
        {
            return new GetStudyTypesQueryResult
            {
                ConfigTypeMetadata = new List<ConfigTypeMetadata>
                {
                    new ConfigTypeMetadata
                    {
                        SingularKey = "a"
                    },
                    new ConfigTypeMetadata
                    {
                        SingularKey = configType
                    },
                }
            };
        }
    }
}