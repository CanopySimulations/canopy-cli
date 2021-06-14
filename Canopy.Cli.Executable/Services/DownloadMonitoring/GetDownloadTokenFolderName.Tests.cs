using Canopy.Cli.Shared;
using Xunit;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class GetDownloadTokenFolderNameTests
    {
        private readonly GetDownloadTokenFolderName target = new();

        [Fact]
        public void WhenNoJobNameItShouldReturnSensibleName()
        {
            var queuedToken = QueuedDownloadToken.Random() with
            {
                Token = DownloadToken.Random() with
                {
                    StudyName = "Study Name",
                    Job = null
                }
            };

            var result = this.target.Execute(queuedToken);

            Assert.Equal("Study Name", result);
        }

        [Fact]
        public void WhenJobNameItShouldReturnSensibleName()
        {
            var queuedToken = QueuedDownloadToken.Random() with
            {
                Token = DownloadToken.Random() with
                {
                    StudyName = "Study Name",
                    Job = DownloadTokenJob.Random() with
                    {
                        JobName = "Job Name"
                    }
                }
            };

            var result = this.target.Execute(queuedToken);

            Assert.Equal("Study Name - Job Name", result);
        }
    }
}