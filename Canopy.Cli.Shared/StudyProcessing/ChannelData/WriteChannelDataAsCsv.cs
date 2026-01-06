#nullable enable
namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class WriteChannelDataAsCsv
    {
        public static async Task ExecuteAsync(
            IRootFolder root,
            IFileWriter writer,
            bool deleteProcessedFiles,
            int parallelism,
            ChannelDataColumns channelDataColumns,
            string? xDomainFilter = null)
        {
            foreach (var simType in channelDataColumns.SimTypes)
            {
                var columns = channelDataColumns.GetColumns(simType);
                var folderGroups = columns.GroupBy(v => v.File.RelativePathToFile);

                foreach (var folderGroup in folderGroups)
                {
                    var relativePathToFile = folderGroup.Key;
                    var metadata = await GetSimTypeMetadataAsync(root, relativePathToFile, simType);

                    var xDomainGroups = folderGroup.GroupBy(v => metadata.GetChannelXDomain(v.Metadata.ChannelName));

                    foreach (var xDomainGroup in xDomainGroups)
                    {
                        var xDomain = xDomainGroup.Key.Trim();

                        // Filter by X-domain if specified
                        if (!string.IsNullOrEmpty(xDomainFilter) &&
                            !string.Equals(xDomain, xDomainFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var fileSuffix = "_" + (string.IsNullOrWhiteSpace(xDomain) ? "Unspecified" : xDomain);

                        var resolvedData = new ConcurrentQueue<ResolvedCsvColumn>();
                        await xDomainGroup.ForEachAsync(
                            parallelism,
                            async column =>
                            {
                                try
                                {
                                    var pointsInChannel = metadata.GetPointsInChannel(column.Metadata.ChannelName);
                                    var buffer = await column.File.GetContentAsBytesAsync();
                                    if (buffer.Length == pointsInChannel * 4)
                                    {
                                        var floatValues = new float[buffer.Length / sizeof(float)];
                                        Buffer.BlockCopy(buffer, 0, floatValues, 0, buffer.Length);
                                        var values = new double[floatValues.Length];
                                        for (int i = 0; i < floatValues.Length; i++)
                                        {
                                            values[i] = (double)floatValues[i];
                                        }
                                        resolvedData.Enqueue(
                                            new ResolvedCsvColumn(column.File, column.Metadata.ChannelName, values));
                                    }
                                    else
                                    {
                                        var values = new double[buffer.Length / sizeof(double)];
                                        Buffer.BlockCopy(buffer, 0, values, 0, buffer.Length);
                                        resolvedData.Enqueue(
                                            new ResolvedCsvColumn(column.File, column.Metadata.ChannelName, values));
                                    }
                                }
                                catch (Exception t)
                                {
                                    writer.ReportError(
                                        "Failed to parse file: " + column.File.FullPath,
                                        t);
                                }
                            });

                        var data = resolvedData.ToList();
                        if (data.Count > 0)
                        {
                            await WriteVectorResultsCsvAsync(
                                root,
                                writer,
                                deleteProcessedFiles,
                                relativePathToFile,
                                simType,
                                fileSuffix,
                                metadata,
                                data);
                        }
                    }
                }
            }
        }

        public static async Task ExecuteAsync(
            IRootFolder root,
            IFileWriter writer,
            bool deleteProcessedFiles,
            int parallelism,
            ChannelDataFiles channelDataColumns,
            string? simTypeFilter = null,
            string? xDomainFilter = null)
        {
            foreach (var simType in channelDataColumns.SimTypes)
            {
                // early exit to avoid unnecessary processing
                // else all sim types would be processed and only writer.WriteNewFile (line 322) actually filters out which file is written
                if (!string.IsNullOrEmpty(simTypeFilter) && !simType.Equals(simTypeFilter, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                var columns = channelDataColumns.GetColumns(simType);
                var folderGroups = columns.GroupBy(v => v.File.RelativePathToFile);

                foreach (var folderGroup in folderGroups)
                {
                    var relativePathToFile = folderGroup.Key;
                    var metadata = await GetSimTypeMetadataAsync(root, relativePathToFile, simType);

                    foreach (var domain in folderGroup)
                    {
                        var xDomain = domain.Domain.Trim();

                        // Filter by X-domain if specified
                        if (!string.IsNullOrEmpty(xDomainFilter) &&
                            !string.Equals(xDomain, xDomainFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var fileSuffix = "_" + (string.IsNullOrWhiteSpace(xDomain) ? "Unspecified" : xDomain);

                        var data = new List<ResolvedCsvColumn>();
                        try
                        {
                            var parquetBytes = await domain.File.GetContentAsBytesAsync();

                            var channelData = await TelemetryChannelSerializer.ConvertChannelsAsync(
                                parquetBytes,
                                new DoubleChannelValueConverter());

                            foreach (var kvp in channelData)
                            {
                                data.Add(new ResolvedCsvColumn(
                                    domain.File,
                                    kvp.Key,
                                    kvp.Value));
                            }
                        }
                        catch (Exception t)
                        {
                            writer.ReportError(
                                "Failed to parse parquet file: " + domain.File.FullPath,
                                t);
                            continue;
                        }
                        if (data.Count > 0)
                        {
                            await WriteVectorResultsCsvAsync(
                                root,
                                writer,
                                deleteProcessedFiles,
                                relativePathToFile,
                                simType,
                                fileSuffix,
                                metadata,
                                data);
                        }
                    }
                }
            }
        }

        private static async Task<SimTypeMetadataResult> GetSimTypeMetadataAsync(
            IRootFolder baseDirectory,
            string relativePathToFile,
            string simType)
        {
            var lookup = new Dictionary<string, SimTypeMetadataRow>();
            try
            {
                var text = await baseDirectory.GetContentAsTextAsync(
                    relativePathToFile, simType + "_VectorMetadata.csv");
                if (text != null)
                {
                    var lines = text.SplitLines();
                    foreach (var line in lines.Skip(1))
                    {
                        try
                        {
                            var values = line.SplitCsvLine().ToList();
                            var units = values.Count >= 2 ? values[1].WithoutQuotes() : null;
                            var xDomain = values.Count >= 5 ? values[4].WithoutQuotes() : null;
                            var pointsInChannel = 0;
                            if (values.Count >= 4)
                            {
                                var pointsInChannelString = values[3].WithoutQuotes();
                                int.TryParse(pointsInChannelString, out pointsInChannel);
                            }
                            lookup[values[0].WithoutQuotes()] = new SimTypeMetadataRow(units, xDomain, pointsInChannel);
                        }
                        catch (Exception t)
                        {
                            // Ignore any errors.
                            Console.WriteLine(t);
                        }
                    }
                }
            }
            catch (Exception t)
            {
                // Ignore any errors.
                Console.WriteLine(t);
            }

            return new SimTypeMetadataResult(lookup);
        }

        public class SimTypeMetadataResult
        {
            private Dictionary<string, SimTypeMetadataRow> lookup;

            public SimTypeMetadataResult(
                Dictionary<string, SimTypeMetadataRow> lookup)
            {
                this.lookup = lookup;
            }

            public string GetChannelUnits(string channelName)
            {
                this.lookup.TryGetValue(channelName, out var row);
                return row?.Units ?? string.Empty;
            }

            public string GetChannelXDomain(string channelName)
            {
                this.lookup.TryGetValue(channelName, out var row);
                return row?.XDomain ?? string.Empty;
            }

            public int GetPointsInChannel(string channelName)
            {
                this.lookup.TryGetValue(channelName, out var row);
                return row?.PointsInChannel ?? 0;
            }
        }

        public class SimTypeMetadataRow
        {
            public SimTypeMetadataRow(string units, string xDomain, int pointsInChannel)
            {
                this.Units = units;
                this.XDomain = xDomain;
                this.PointsInChannel = pointsInChannel;
            }

            public string Units { get; }
            public string XDomain { get; }
            public int PointsInChannel { get; }
        }

        private static async Task WriteVectorResultsCsvAsync(
            IRootFolder root,
            IFileWriter writer,
            bool deleteProcessedFiles,
            string relativePathToFile,
            string simType,
            string fileSuffix,
            SimTypeMetadataResult metadata,
            List<ResolvedCsvColumn> data)
        {
            const string AtlasPrimaryChannel = "tRun";
            data.Sort((a, b) =>
            {
                if (a == b)
                {
                    return 0;
                }

                if (a.ChannelName == AtlasPrimaryChannel)
                {
                    return -1;
                }

                if (b.ChannelName == AtlasPrimaryChannel)
                {
                    return 1;
                }

                return String.Compare(a.ChannelName, b.ChannelName, StringComparison.OrdinalIgnoreCase);
            });

            var maxDataLength = data.Select(v => v.Data.Length).Max();
            var csv = new StringBuilder();
            csv.AppendLine(relativePathToFile + simType);
            csv.AppendLine(string.Join(",", data.Select(v => v.ChannelName)));
            csv.AppendLine(string.Join(",", data.Select(v =>
            {
                var units = metadata.GetChannelUnits(v.ChannelName);
                if (string.IsNullOrWhiteSpace(units))
                {
                    return "\"()\"";
                }

                return "\"" + units + "\"";
            })));
            Console.WriteLine($"MaxDataLength={maxDataLength}");

            for (int i = 0; i < maxDataLength; i++)
            {
                csv.AppendLine(
                    string.Join(
                        ",",
                        data.Select(v => v.Data.Length > i ? v.Data[i].NumericOrNaN().ToString(CultureInfo.InvariantCulture) : "").ToList()));
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            var fileName = simType + "_VectorResults" + fileSuffix + ".csv";
            if (string.IsNullOrWhiteSpace(relativePathToFile))
            {
                Console.WriteLine($"Writing '{fileName}'. Path is empty.");
            }
            else
            {
                Console.WriteLine($"Writing '{fileName}' to '{relativePathToFile}'.");
            }

            await writer.WriteNewFile(root, relativePathToFile, fileName, bytes);

            if (deleteProcessedFiles)
            {
                foreach (var column in data)
                {
                    await writer.DeleteProcessedFile(root, column.File);
                }
            }
        }

    }
}