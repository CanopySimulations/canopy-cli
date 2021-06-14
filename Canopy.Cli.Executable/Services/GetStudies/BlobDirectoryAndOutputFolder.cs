using Microsoft.Azure.Storage.Blob;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public record BlobDirectoryAndOutputFolder(CloudBlobDirectory Directory, string OutputFolder);
}