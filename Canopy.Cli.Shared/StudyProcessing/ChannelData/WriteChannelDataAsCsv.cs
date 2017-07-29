namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
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
                var groupedColumns = columns.GroupBy(v => v.File.RelativePathToFile);

                foreach (var group in groupedColumns)
                {
                    var relativePathToFile = group.Key;
                    var unitsLookupTask = GetUnitsLookup(root, relativePathToFile, simType);

                    var resolvedData = new ConcurrentQueue<ResolvedCsvColumn>();
                    await group.ForEachAsync(
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

                    var unitsLookup = await unitsLookupTask;

                    var data = resolvedData.ToList();
                    if (data.Count > 0)
                    {
                        data.Sort((a, b) => String.Compare(a.ChannelName, b.ChannelName, StringComparison.OrdinalIgnoreCase));

                        var firstItemLength = data[0].Data.Length;
                        var excludedData = data.Where(v => v.Data.Length != firstItemLength).Select(v => v.ChannelName)
                            .ToList();

                        if (excludedData.Count > 0)
                        {
                            writer.ReportError(
                                "Excluding channels from CSV download due to incorrect length: " +
                                string.Join(",", excludedData),
                                null);
                        }

                        data = data.Where(v => v.Data.Length == firstItemLength).ToList();
                        var csv = new StringBuilder();
                        csv.AppendLine(relativePathToFile + simType);
                        csv.AppendLine(string.Join(",", data.Select(v => v.ChannelName)));
                        csv.AppendLine(string.Join(",", data.Select(v =>
                        {
                            string units;
                            unitsLookup.TryGetValue(v.ChannelName, out units);
                            if (string.IsNullOrWhiteSpace(units))
                            {
                                return "\"()\"";
                            }

                            return "\"" + units + "\"";
                        })));

                        for (int i = 0; i < firstItemLength; i++)
                        {
                            csv.AppendLine(string.Join(",", data.Select(v => v.Data[i])));
                        }

                        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
                        await writer.WriteNewFile(root, relativePathToFile, simType + "_VectorResults.csv", bytes);

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

        private static async Task<Dictionary<string, string>> GetUnitsLookup(
            IRootFolder baseDirectory,
            string relativePathToFile, 
            string simType)
        {
            var unitsLookup = new Dictionary<string, string>();
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

            return unitsLookup;
        }
    }
}