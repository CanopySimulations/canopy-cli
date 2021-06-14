using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Canopy.Api.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public class GetAllRequiredDirectoryMetadata : IGetAllRequiredDirectoryMetadata
    {
        public IReadOnlyList<BlobAccessInformationAndOutputFolder> Execute(
            GetStudyQueryResult studyMetadata,
            string outputFolder,
            int? jobIndex)
        {
            var accessInformation = studyMetadata.AccessInformation;

            var mainDirectory = new BlobAccessInformation(accessInformation.Url, accessInformation.AccessSignature);

            var studyData = studyMetadata.Study.Data as JObject;
            Guard.Operation(studyData != null, "Study data was not found in study metadata result.");

            var jobCount = studyData.Value<int>(Api.Client.Constants.JobCountKey);
            var jobDirectoryCount = Math.Min(jobCount, accessInformation.Jobs.Count);
            var jobDirectories = accessInformation.Jobs.Take(jobDirectoryCount)
                .Select(v => new BlobAccessInformation(v.Url, v.AccessSignature))
                .ToList();

            var result = new List<BlobAccessInformationAndOutputFolder>
            {
                new BlobAccessInformationAndOutputFolder(mainDirectory, outputFolder),
            };

            if (jobIndex.HasValue)
            {
                var jobAccessInformation = jobDirectories[jobIndex.Value % jobDirectoryCount];
                var jobFolderName = jobIndex.Value.ToString();
                result.Add(
                    new BlobAccessInformationAndOutputFolder(
                        new BlobAccessInformation(
                            UrlUtilities.AppendFolderToUrl(jobAccessInformation.Url, jobFolderName),
                            jobAccessInformation.AccessSignature),
                        Path.Combine(outputFolder, jobFolderName)));
            }
            else
            {
                result.AddRange(jobDirectories.Select(v => new BlobAccessInformationAndOutputFolder(v, outputFolder)));
            }

            return result;
        }
    }
}