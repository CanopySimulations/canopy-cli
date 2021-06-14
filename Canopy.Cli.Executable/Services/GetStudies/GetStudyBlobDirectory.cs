using Canopy.Api.Client;
using System;
using Microsoft.Azure.Storage.Blob;
using System.Text.RegularExpressions;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public class GetStudyBlobDirectory : IGetStudyBlobDirectory
    {
        public CloudBlobDirectory Execute(BlobAccessInformation accessInformation)
        {
            return this.Execute(accessInformation.Url, accessInformation.AccessSignature);
        }

        public CloudBlobDirectory Execute(string url, string accessSignature)
        {
            const string containerUrlKey = "containerUrl";
            const string studyPathKey = "studyPath";
            var containerUrlMatch = Regex.Match(url, $@"^(?<{containerUrlKey}>https://[^/]*/[\w]*)/(?<{studyPathKey}>.*)$");
            if (!containerUrlMatch.Success)
            {
                throw new RecoverableException("Unexpected study URL format: " + url);
            }

            var containerUrl = containerUrlMatch.Groups[containerUrlKey].Value;
            var studyPath = containerUrlMatch.Groups[studyPathKey].Value;

            var container = new CloudBlobContainer(new Uri(containerUrl + accessSignature));

            var directory = container.GetDirectoryReference(studyPath);
            return directory;
        }
    }
}