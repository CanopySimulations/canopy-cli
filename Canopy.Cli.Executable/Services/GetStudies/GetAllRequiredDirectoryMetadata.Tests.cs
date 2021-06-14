using System.Collections.Generic;
using System.IO;
using System.Linq;
using Canopy.Api.Client;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public class GetAllRequiredDirectoryMetadataTests
    {
        private const string OutputFolder = "./studies";

        private const string StudyUrl = "https://s.foo.com/bar";
        private const string JobUrl0 = "https://j0.foo.com/bar";
        private const string JobUrl1 = "https://j1.foo.com/bar";
        private const string JobUrl2 = "https://j2.foo.com/bar";

        private readonly GetAllRequiredDirectoryMetadata target = new GetAllRequiredDirectoryMetadata();

        [Fact]
        public void WhenFewerJobsThanShardsItShouldOnlyReturnRequiredDirectories()
        {
            var studyMetadata = this.CreateStudyResult(2);
            var result = this.target.Execute(
                studyMetadata,
                OutputFolder,
                null);

            Assert.Equal(3, result.Count);

            Assert.True(result.All(v => v.OutputFolder == OutputFolder));
            Assert.Equal(StudyUrl, result[0].AccessInformation.Url);
            Assert.Equal(JobUrl0, result[1].AccessInformation.Url);
            Assert.Equal(JobUrl1, result[2].AccessInformation.Url);
        }

        [Fact]
        public void WhenMoreJobsThanShardsItShouldReturnAllDirectories()
        {
            var studyMetadata = this.CreateStudyResult(10);
            var result = this.target.Execute(
                studyMetadata,
                OutputFolder,
                null);

            Assert.Equal(4, result.Count);

            Assert.True(result.All(v => v.OutputFolder == OutputFolder));
            Assert.Equal(StudyUrl, result[0].AccessInformation.Url);
            Assert.Equal(JobUrl0, result[1].AccessInformation.Url);
            Assert.Equal(JobUrl1, result[2].AccessInformation.Url);
            Assert.Equal(JobUrl2, result[3].AccessInformation.Url);
        }

        [Fact]
        public void WhenJobIndexSpecifiedFewerJobsThanShardsItShouldOnlyReturnRequiredDirectories()
        {
            var studyMetadata = this.CreateStudyResult(2);
            var result = this.target.Execute(
                studyMetadata,
                OutputFolder,
                1);

            Assert.Equal(2, result.Count);

            Assert.Equal(OutputFolder, result[0].OutputFolder);
            Assert.Equal(StudyUrl, result[0].AccessInformation.Url);

            Assert.Equal(Path.Combine(OutputFolder, "1"), result[1].OutputFolder);
            Assert.Equal(JobUrl1 + "/1/", result[1].AccessInformation.Url);
        }

        [Fact]
        public void WhenJobIndexSpecifiedAndMoreJobsThanShardsItShouldOnlyReturnRequiredDirectories()
        {
            var studyMetadata = this.CreateStudyResult(10);
           
            var result = this.target.Execute(
                studyMetadata,
                OutputFolder,
                8);

            Assert.Equal(2, result.Count);

            Assert.Equal(OutputFolder, result[0].OutputFolder);
            Assert.Equal(StudyUrl, result[0].AccessInformation.Url);

            Assert.Equal(Path.Combine(OutputFolder, "8"), result[1].OutputFolder);
            Assert.Equal(JobUrl2 + "/8/", result[1].AccessInformation.Url);
        }

        [Fact]
        public void WhenJobIndexSpecifiedAndMoreJobsThanShardsItShouldOnlyReturnRequiredDirectories2()
        {
            var studyMetadata = this.CreateStudyResult(10);
           
            var result = this.target.Execute(
                studyMetadata,
                OutputFolder,
                6);

            Assert.Equal(2, result.Count);

            Assert.Equal(OutputFolder, result[0].OutputFolder);
            Assert.Equal(StudyUrl, result[0].AccessInformation.Url);

            Assert.Equal(Path.Combine(OutputFolder, "6"), result[1].OutputFolder);
            Assert.Equal(JobUrl0 + "/6/", result[1].AccessInformation.Url);
        }

        private GetStudyQueryResult CreateStudyResult(int jobCount)
        {
            return new GetStudyQueryResult
            {
                AccessInformation = new StudyBlobAccessInformation
                {
                    Url = StudyUrl,
                    AccessSignature = "study-signature",
                    Jobs = new List<Api.Client.BlobAccessInformation>
                    {
                        new Api.Client.BlobAccessInformation
                        {
                            Url = JobUrl0,
                            AccessSignature = "job-0-signature",
                        },
                        new Api.Client.BlobAccessInformation
                        {
                            Url = JobUrl1,
                            AccessSignature = "job-1-signature",
                        },
                        new Api.Client.BlobAccessInformation
                        {
                            Url = JobUrl2,
                            AccessSignature = "job-2-signature",
                        },
                    }
                },
                Study = new CanopyDocument
                {
                    Data = new JObject(
                        new JProperty(Api.Client.Constants.JobCountKey, jobCount)),
                }
            };
        }
    }
}