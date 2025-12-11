#nullable enable
using Parquet;
using System;
using System.Collections.Generic;
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

                            using (var memoryStream = new MemoryStream(parquetBytes))
                            {
                                using (var parquetReader = await ParquetReader.CreateAsync(memoryStream))
                                {
                                    var dataFields = parquetReader.Schema.GetDataFields();

                                    // Dictionary to accumulate all values for each channel
                                    var channelData = new Dictionary<string, List<float>>();

                                    // Read all row groups
                                    for (int i = 0; i < parquetReader.RowGroupCount; i++)
                                    {
                                        using (var rowGroupReader = parquetReader.OpenRowGroupReader(i))
                                        {
                                            foreach (var field in dataFields)
                                            {
                                                var columnData = await rowGroupReader.ReadColumnAsync(field);

                                                // Initialize list for this channel if first row group
                                                if (!channelData.ContainsKey(field.Name))
                                                {
                                                    channelData[field.Name] = new List<float>();
                                                }

                                                // Convert column values to floats and add to list
                                                for (int j = 0; j < columnData.Data.Length; j++)
                                                {
                                                    var value = columnData.Data.GetValue(j);
                                                    var floatValue = value switch
                                                    {
                                                        float f => f,
                                                        double d => (float)d,
                                                        int n => (float)n,
                                                        long l => (float)l,
                                                        _ => float.NaN,
                                                    };
                                                    channelData[field.Name].Add(floatValue);
                                                }
                                            }
                                        }
                                    }

                                    // Write each channel as a separate .bin file
                                    foreach (var kvp in channelData)
                                    {
                                        var channelName = kvp.Key;
                                        var channelValues = kvp.Value.ToArray();

                                        // Convert float array to byte array
                                        var byteArray = new byte[channelValues.Length * sizeof(float)];
                                        Buffer.BlockCopy(channelValues, 0, byteArray, 0, byteArray.Length);

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
