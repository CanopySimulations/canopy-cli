using System.Linq;

namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    public static class TryGetChannelMetadata
    {
        public static bool Execute(IFile file, out BlobChannelMetadata result)
        {
            result = null;
            const string channelNameFileExtension = ".bin";

            var fileName = file.FileName;
            if (!fileName.EndsWith(channelNameFileExtension))
            {
                return false;
            }

            fileName = fileName.Substring(0, fileName.Length - channelNameFileExtension.Length);

            var parts = fileName.Split('_');
            if (parts.Length < 2)
            {
                return false;
            }

            var simType = parts[0];
            var channelName = fileName.Substring(simType.Length + 1);

            result = new BlobChannelMetadata(simType, channelName);
            return true;
        }
    }
}