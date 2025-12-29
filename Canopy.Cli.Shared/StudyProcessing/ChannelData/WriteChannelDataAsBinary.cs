#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    public static class WriteChannelDataAsBinary
    {
        public static async Task ExecuteAsync(
            IRootFolder root,
            IFileWriter writer,
            bool deleteProcessedFiles,
            int parallelism,
            ChannelDataFiles channelDataFiles,
            string? xDomainFilter = null)
        {
            foreach (var simType in channelDataFiles.SimTypes)
            {
                var columns = channelDataFiles.GetColumns(simType);
                var folderGroups = columns.GroupBy(v => v.File.RelativePathToFile);

                foreach (var folderGroup in folderGroups)
                {
                    var relativePathToFile = folderGroup.Key;

                    foreach (var domain in folderGroup)
                    {
                        var xDomain = domain.Domain.Trim();

                        // Filter by X-domain if specified
                        if (!string.IsNullOrEmpty(xDomainFilter) &&
                            !string.Equals(xDomain, xDomainFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        try
                        {
                            // Read the parquet file containing all channels for this xDomain
                            var parquetBytes = await domain.File.GetContentAsBytesAsync();

                            // Use shared converter to extract all channels
                            var channelDataDict = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes);

                            // Write each channel as a separate .bin file
                            foreach (var kvp in channelDataDict)
                            {
                                var channelName = kvp.Key;
                                var byteArray = kvp.Value;

                                // Create filename with channel name
                                var fileName = $"{simType}_{channelName}_1.bin";

                                Console.WriteLine($"Writing channel '{channelName}' to '{fileName}' in '{relativePathToFile}'.");

                                await writer.WriteNewFile(root, relativePathToFile, fileName, byteArray);
                            }

                            if (deleteProcessedFiles)
                            {
                                await writer.DeleteProcessedFile(root, domain.File);
                            }
                        }
                        catch (Exception t)
                        {
                            writer.ReportError(
                                "Failed to process parquet file: " + domain.File.FullPath,
                                t);
                        }
                    }
                }
            }
        }
    }
}
