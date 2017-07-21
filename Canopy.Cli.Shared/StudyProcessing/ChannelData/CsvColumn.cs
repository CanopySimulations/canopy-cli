namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    public class CsvColumn
    {
        public CsvColumn(BlobChannelMetadata metadata, IFile file)
        {
            this.Metadata = metadata;
            this.File = file;
        }

        public BlobChannelMetadata Metadata { get; }

        public IFile File { get; }
    }
}