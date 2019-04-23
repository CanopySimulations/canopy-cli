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
            ChannelDataColumns channelDataColumns)
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
                                            values[i] = (double) floatValues[i];
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
                            // Always put sLap at the start for ATLAS compatibility.
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

                            for (int i = 0; i < maxDataLength; i++)
                            {
                                csv.AppendLine(
                                    string.Join(
                                        ",",
                                        data.Select(v => v.Data.Length > i ? v.Data[i].NumericOrNaN().ToString(CultureInfo.InvariantCulture) : "").ToList()));
                            }

                            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                            var fileName = simType + "_VectorResults" + fileSuffix + ".csv";
                            Console.WriteLine($"Writing '{fileName}' to '{relativePathToFile}'.");
                            await writer.WriteNewFile(root, relativePathToFile, simType + "_VectorResults" + fileSuffix +  ".csv", bytes);

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
    }
}