#nullable enable
using Parquet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    /// <summary>
    /// Utility for serializing telemetry channel data from Parquet format to binary format.
    /// </summary>
    public static class TelemetryChannelSerializer
    {
        /// <summary>
        /// Converts channels from Parquet bytes to a dictionary of channel name to typed arrays using the provided converter.
        /// </summary>
        public static async Task<Dictionary<string, T[]>> ConvertChannelsAsync<T>(
            byte[] parquetBytes,
            IChannelValueConverter<T> converter,
            IReadOnlyList<string>? channelNames = null)
        {
            ArgumentNullException.ThrowIfNull(parquetBytes);
            ArgumentNullException.ThrowIfNull(converter);

            using var memoryStream = new MemoryStream(parquetBytes);
            using var parquetReader = await ParquetReader.CreateAsync(memoryStream).ConfigureAwait(false);
            var channelDataDict = await ExtractChannelsAsync(parquetReader, converter, channelNames).ConfigureAwait(false);
            var result = new Dictionary<string, T[]>(channelDataDict.Count);

            foreach (var kvp in channelDataDict)
            {
                result[kvp.Key] = [.. kvp.Value];
            }

            return result;
        }

        /// <summary>
        /// Converts channels from a Parquet stream, streaming each channel's typed data as it's converted.
        /// </summary>
        public static async IAsyncEnumerable<(string Name, T[] Data)> ConvertChannelsStreamAsync<T>(
            byte[] parquetBytes,
            IChannelValueConverter<T> converter,
            IReadOnlyList<string>? channelNames,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(parquetBytes);
            ArgumentNullException.ThrowIfNull(converter);

            using var memoryStream = new MemoryStream(parquetBytes);
            using var parquetReader = await ParquetReader.CreateAsync(memoryStream, cancellationToken: cancellationToken).ConfigureAwait(false);
     
            // Optimization: For single row group, stream channels one at a time for better memory efficiency
            if (parquetReader.RowGroupCount == 1)
            {
                var channelNamesSet = channelNames != null && channelNames.Count > 0
                ? new HashSet<string>(channelNames)
                : null;

                var dataFields = channelNamesSet != null
                        ? parquetReader.Schema.GetDataFields().Where(f => channelNamesSet.Contains(f.Name))
                        : parquetReader.Schema.GetDataFields();

                using var rowGroupReader = parquetReader.OpenRowGroupReader(0);

                foreach (var field in dataFields)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var columnData = await rowGroupReader.ReadColumnAsync(field).ConfigureAwait(false);
                    var channelValues = new List<T>(columnData.Data.Length);
                    ConvertAndAddValues(columnData, converter, channelValues);

                    yield return (field.Name, channelValues.ToArray());
                }
            }

            else
            {
               var channelData = await ExtractChannelsAsync(parquetReader, converter, channelNames).ConfigureAwait(false);

                foreach (var kvp in channelData)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return (kvp.Key, kvp.Value.ToArray());
                }
            }            
        }

        /// <summary>
        /// Extracts channels from a Parquet stream.
        /// </summary>
        private static async Task<Dictionary<string, List<T>>> ExtractChannelsAsync<T>(
            ParquetReader parquetReader,
            IChannelValueConverter<T> converter,
            IReadOnlyList<string>? channelNames)
        {
            var channelNamesSet = channelNames != null && channelNames.Count > 0
                ? new HashSet<string>(channelNames)
                : null;

            var channelData = new Dictionary<string, List<T>>();
            var dataFields = channelNamesSet != null
                    ? parquetReader.Schema.GetDataFields().Where(f => channelNamesSet.Contains(f.Name))
                    : parquetReader.Schema.GetDataFields();

            for (int i = 0; i < parquetReader.RowGroupCount; i++)
            {
                using var rowGroupReader = parquetReader.OpenRowGroupReader(i);

                foreach (var field in dataFields)
                {
                    var columnData = await rowGroupReader.ReadColumnAsync(field).ConfigureAwait(false);

                    if (!channelData.TryGetValue(field.Name, out var list))
                    {
                        list = new List<T>();
                        channelData[field.Name] = list;
                    }

                    ConvertAndAddValues(columnData, converter, list);
                }
            }

            return channelData;
        }

        /// <summary>
        /// Converts column data values using the converter and adds them to the list.
        /// </summary>
        private static void ConvertAndAddValues<T>(Parquet.Data.DataColumn columnData, IChannelValueConverter<T> converter, List<T> targetList)
        {
            var data = columnData.Data;

            for (int j = 0; j < data.Length; j++)
            {
                targetList.Add(converter.Convert(data.GetValue(j)));
            }
        }
    }
}
