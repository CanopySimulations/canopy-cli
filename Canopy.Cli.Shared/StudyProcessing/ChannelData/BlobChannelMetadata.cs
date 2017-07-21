namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    public class BlobChannelMetadata
    {
        public BlobChannelMetadata(string simType, string channelName)
        {
            this.SimType = simType;
            this.ChannelName = channelName;
        }

        public string SimType { get; }

        public string ChannelName { get; }
    }
}