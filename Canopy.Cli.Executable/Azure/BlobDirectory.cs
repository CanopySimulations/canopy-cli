using Azure.Storage.Blobs;

namespace Canopy.Cli.Executable.Azure
{
    public record BlobDirectory(BlobContainerClient Container, string Prefix);
}
