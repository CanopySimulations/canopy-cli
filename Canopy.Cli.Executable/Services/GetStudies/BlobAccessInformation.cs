using Microsoft.Azure.Storage.Blob;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public record BlobAccessInformation(string Url, string AccessSignature);
}