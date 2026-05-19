using Canopy.Cli.Executable.Azure;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public record BlobDirectoryAndOutputFolder(BlobDirectory Directory, string OutputFolder);
}