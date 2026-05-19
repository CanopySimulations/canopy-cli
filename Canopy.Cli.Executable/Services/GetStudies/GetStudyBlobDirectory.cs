using Canopy.Api.Client;
using System;
using Azure.Storage.Blobs;
using System.Text.RegularExpressions;
using Canopy.Cli.Executable.Azure;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public class GetStudyBlobDirectory : IGetStudyBlobDirectory
    {
        public BlobDirectory Execute(BlobAccessInformation accessInformation)
        {
            return this.Execute(accessInformation.Url, accessInformation.AccessSignature);
        }

        public BlobDirectory Execute(string url, string accessSignature)
        {
            const string containerUrlKey = "containerUrl";
            const string studyPathKey = "studyPath";
            var containerUrlMatch = Regex.Match(url, $@"^(?<{containerUrlKey}>https://[^/]*/[\w]*)/(?<{studyPathKey}>.+?)/?$");
            if (!containerUrlMatch.Success)
            {
                throw new RecoverableException("Unexpected study URL format: " + url);
            }

            var containerUrl = containerUrlMatch.Groups[containerUrlKey].Value;
            var studyPath = containerUrlMatch.Groups[studyPathKey].Value;

            var container = new BlobContainerClient(new Uri(containerUrl + accessSignature));

            return new BlobDirectory(container, studyPath);
        }
    }
}