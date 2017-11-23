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
                                    var buffer = await column.File.GetContentAsBytesAsync();
                                    double[] values = new double[buffer.Length / sizeof(double)];
                                    Buffer.BlockCopy(buffer, 0, values, 0, buffer.Length);
                                    resolvedData.Enqueue(
                                        new ResolvedCsvColumn(column.File, column.Metadata.ChannelName, values));
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
                            data.Sort((a, b) => String.Compare(a.ChannelName, b.ChannelName, StringComparison.OrdinalIgnoreCase));

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
            var unitsLookup = new Dictionary<string, string>();
            var xDomainLookup = new Dictionary<string, string>();
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
                            if (values.Count >= 2)
                            {
                                unitsLookup[values[0].WithoutQuotes()] = values[1].WithoutQuotes();
                            }
                            if (values.Count >= 5)
                            {
                                xDomainLookup[values[0].WithoutQuotes()] = values[4].WithoutQuotes();
                            }
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

            return new SimTypeMetadataResult(unitsLookup, xDomainLookup);
        }

        public class SimTypeMetadataResult
        {
            private Dictionary<string, string> unitsLookup;

            private Dictionary<string, string> xDomainLookup;

            public SimTypeMetadataResult(Dictionary<string, string> unitsLookup, Dictionary<string, string> xDomainLookup)
            {
                this.unitsLookup = unitsLookup;
                this.xDomainLookup = xDomainLookup;
            }

            public string GetChannelUnits(string channelName)
            {
                this.unitsLookup.TryGetValue(channelName, out var units);
                return units ?? string.Empty;
            }

            public string GetChannelXDomain(string channelName)
            {
                this.xDomainLookup.TryGetValue(channelName, out var domain);
                return domain ?? string.Empty;
            }
        }
    }
}