namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    public class ResolvedCsvColumn
    {
        public ResolvedCsvColumn(IFile file, string channelName, double[] data)
        {
            this.File = file;
            this.ChannelName = channelName;
            this.Data = data;
        }

        public IFile File { get; }

        public string ChannelName { get; }

        public double[] Data { get; }
    }
}