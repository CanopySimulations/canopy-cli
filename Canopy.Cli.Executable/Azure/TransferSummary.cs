namespace Canopy.Cli.Executable.Azure
{
    public record TransferSummary(long BytesTransferred, long FilesTransferred, long FilesFailed, long FilesSkipped);
}
