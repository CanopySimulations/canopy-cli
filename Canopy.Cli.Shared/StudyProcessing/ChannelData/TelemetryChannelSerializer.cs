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
        /// Converts channels from Parquet bytes to a dictionary of channel name to binary data.
        /// </summary>
        /// <param name="parquetBytes">The Parquet file bytes.</param>
        /// <param name="channelNames">Optional list of specific channel names to extract. If null or empty, extracts all channels.</param>
        /// <returns>Dictionary mapping channel names to their binary data. Missing channels are excluded.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parquetBytes is null.</exception>
        public static async Task<Dictionary<string, byte[]>> ConvertChannelsAsync(byte[] parquetBytes, IReadOnlyList<string>? channelNames = null)
        {
            ArgumentNullException.ThrowIfNull(parquetBytes);

            using var memoryStream = new MemoryStream(parquetBytes);
            return await ConvertChannelsAsync(memoryStream, channelNames).ConfigureAwait(false);
        }

        /// <summary>
        /// Converts channels from a Parquet stream to a dictionary of channel name to binary data.
        /// </summary>
        /// <param name="parquetStream">The Parquet file stream.</param>
        /// <param name="channelNames">Optional list of specific channel names to extract. If null or empty, extracts all channels.</param>
        /// <returns>Dictionary mapping channel names to their binary data. Missing channels are excluded.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parquetStream is null.</exception>
        public static async Task<Dictionary<string, byte[]>> ConvertChannelsAsync(Stream parquetStream, IReadOnlyList<string>? channelNames = null)
        {
            ArgumentNullException.ThrowIfNull(parquetStream);

            var channelDataDict = await ExtractChannelsAsync(parquetStream, channelNames).ConfigureAwait(false);
            var result = new Dictionary<string, byte[]>(channelDataDict.Count);

            foreach (var kvp in channelDataDict)
            {
                result[kvp.Key] = ConvertFloatsToBinary(kvp.Value);
            }

            return result;
        }

        /// <summary>
        /// Converts channels from a Parquet stream, streaming each channel's binary data as it's converted.
        /// This is more memory-efficient than ConvertChannelsAsync as channels are not held in memory simultaneously.
        /// </summary>
        /// <param name="parquetStream">The Parquet file stream.</param>
        /// <param name="channelNames">Optional list of specific channel names to extract. If null or empty, extracts all channels.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>Async enumerable of channel name and binary data pairs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parquetStream is null.</exception>
        public static async IAsyncEnumerable<(string Name, byte[] Data)> ConvertChannelsStreamAsync(
            Stream parquetStream,
            IReadOnlyList<string>? channelNames,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(parquetStream);

            using var parquetReader = await ParquetReader.CreateAsync(parquetStream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var channelNamesSet = channelNames != null && channelNames.Count > 0
                ? new HashSet<string>(channelNames)
                : null;

            var dataFields = channelNamesSet != null
                    ? parquetReader.Schema.GetDataFields().Where(f => channelNamesSet.Contains(f.Name))
                    : parquetReader.Schema.GetDataFields();

            for (int i = 0; i < parquetReader.RowGroupCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var rowGroupReader = parquetReader.OpenRowGroupReader(i);
                

                foreach (var field in dataFields)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var columnData = await rowGroupReader.ReadColumnAsync(field).ConfigureAwait(false);

                    var floatList = new List<float>();
                    ConvertAndAddValues(columnData, floatList);

                    var binaryData = ConvertFloatsToBinary(floatList);
                    yield return (field.Name, binaryData);
                }
            }
        }

        /// <summary>
        /// Extracts channels from a Parquet stream.
        /// </summary>
        private static async Task<Dictionary<string, List<float>>> ExtractChannelsAsync(Stream parquetStream, IReadOnlyList<string>? channelNames)
        {
            using var parquetReader = await ParquetReader.CreateAsync(parquetStream).ConfigureAwait(false);

            var channelNamesSet = channelNames != null && channelNames.Count > 0
                ? new HashSet<string>(channelNames)
                : null;

            var channelData = new Dictionary<string, List<float>>();
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
                        list = new List<float>();
                        channelData[field.Name] = list;
                    }

                    ConvertAndAddValues(columnData, list);
                }
            }

            return channelData;
        }

        /// <summary>
        /// Converts column data values to floats and adds them to the list.
        /// </summary>
        private static void ConvertAndAddValues(Parquet.Data.DataColumn columnData, List<float> targetList)
        {
            var data = columnData.Data;        

            for (int j = 0; j < data.Length; j++)
            {
                var value = data.GetValue(j);
                var floatValue = value switch
                {
                    float f => f,
                    double d => (float)d,
                    int n => (float)n,
                    long l => (float)l,
                    _ => float.NaN,
                };
                targetList.Add(floatValue);
            }
        }

        /// <summary>
        /// Converts a list of floats to a byte array.
        /// </summary>
        private static byte[] ConvertFloatsToBinary(List<float> floats)
        {
            if (floats.Count == 0)
            {
                return Array.Empty<byte>();
            }

            var byteArray = new byte[floats.Count * sizeof(float)];
            Buffer.BlockCopy(floats.ToArray(), 0, byteArray, 0, byteArray.Length);
            return byteArray;
        }
    }
}
