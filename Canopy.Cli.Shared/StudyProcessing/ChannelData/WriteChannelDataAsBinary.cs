#nullable enable
using System;
using System.IO;
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
            DomainChannelFiles domainChannelFiles,
            string? xDomainFilter = null)
        {
            foreach (var simType in domainChannelFiles.SimTypes)
            {
                var domains = domainChannelFiles.GetDomains(simType);
                var folderGroups = domains.GroupBy(v => v.File.RelativePathToFile);

                foreach (var folderGroup in folderGroups)
                {
                    var relativePathToFile = folderGroup.Key;

                    // Process each domain in parallel using the provided degree of parallelism
                    await folderGroup.ForEachAsync(parallelism, async domain =>
                    {
                        var xDomain = domain.Domain.Trim();

                        // Filter by X-domain if specified
                        if (!string.IsNullOrEmpty(xDomainFilter) &&
                            !string.Equals(xDomain, xDomainFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        try
                        {
                            // Read the parquet file containing all channels for this xDomain
                            var parquetBytes = await domain.File.GetContentAsBytesAsync();

                            var typeConverter = new FloatChannelValueConverter();
                            await foreach (var (channelName, dataArray) in 
                                TelemetryChannelSerializer.ConvertChannelsStreamAsync(parquetBytes, typeConverter, null, default))
                            {
                                var fileName = $"{simType}_{channelName}.bin";

                                Console.WriteLine($"Writing channel '{channelName}' to '{fileName}' in '{relativePathToFile}'.");

                                await writer.WriteNewFile(root, relativePathToFile, fileName, typeConverter.Serialize(dataArray));
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
                    });
                }
            }
        }
    }
}
