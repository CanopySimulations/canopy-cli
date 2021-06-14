using Canopy.Api.Client;
using System.Collections.Generic;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public interface IGetAllRequiredDirectoryMetadata
    {
        IReadOnlyList<BlobAccessInformationAndOutputFolder> Execute(GetStudyQueryResult studyMetadata, string outputFolder, int? jobIndex);
    }
}