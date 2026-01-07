#nullable enable
using Parquet;
using Parquet.Data;
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
        /// <typeparam name="T">The target element type for channel values (e.g. <c>float</c> or <c>double</c>).</typeparam>
        /// <param name="parquetBytes">The Parquet file contents as a byte array.</param>
        /// <param name="converter">A converter that maps raw Parquet values to <typeparamref name="T"/>.</param>
        /// <param name="channelNames">Optional list of channel names to include. If <c>null</c> or empty, all channels are returned.</param>
        /// <returns>
        /// A dictionary mapping channel name to an array of converted values of type <typeparamref name="T"/>.
        /// The returned arrays contain the converted element values (element count = array.Length).
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parquetBytes"/> or <paramref name="converter"/> is <c>null</c>.</exception>
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
        /// <typeparam name="T">The target element type for channel values.</typeparam>
        /// <param name="parquetBytes">The Parquet file contents as a byte array.</param>
        /// <param name="converter">A converter that maps raw Parquet values to <typeparamref name="T"/>.</param>
        /// <param name="channelNames">Optional list of channel names to include. If <c>null</c> or empty, all channels are streamed.</param>
        /// <param name="cancellationToken">Cancellation token to cancel streaming.</param>
        /// <returns>An async stream of tuples containing the channel name and the converted values array.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parquetBytes"/> or <paramref name="converter"/> is <c>null</c>.</exception>
        public static async IAsyncEnumerable<(string Name, T[] Data)> ConvertChannelsStreamAsync<T>(
            byte[] parquetBytes,
            IChannelValueConverter<T> converter,
            IReadOnlyList<string>? channelNames,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(parquetBytes);
            ArgumentNullException.ThrowIfNull(converter);


            using var memoryStream = new MemoryStream(parquetBytes);
            await foreach (var item in ConvertChannelsStreamAsync(memoryStream, converter, channelNames, cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Converts channels from a Parquet stream, streaming each channel's typed data as it's converted.
        /// </summary>
        /// <typeparam name="T">The target element type for channel values.</typeparam>
        /// <param name="parquetStream">The Parquet file contents as Stream.</param>
        /// <param name="converter">A converter that maps raw Parquet values to <typeparamref name="T"/>.</param>
        /// <param name="channelNames">Optional list of channel names to include. If <c>null</c> or empty, all channels are streamed.</param>
        /// <param name="cancellationToken">Cancellation token to cancel streaming.</param>
        /// <returns>An async stream of tuples containing the channel name and the converted values array.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parquetStream"/> or <paramref name="converter"/> is <c>null</c>.</exception>
        public static async IAsyncEnumerable<(string Name, T[] Data)> ConvertChannelsStreamAsync<T>(
            Stream parquetStream,
            IChannelValueConverter<T> converter,
            IReadOnlyList<string>? channelNames,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(parquetStream);
            ArgumentNullException.ThrowIfNull(converter);

            using var parquetReader = await ParquetReader.CreateAsync(parquetStream, cancellationToken: cancellationToken).ConfigureAwait(false);
     
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
        /// Extracts channels from an open <see cref="ParquetReader"/> across all row groups.
        /// </summary>
        /// <typeparam name="T">The target element type produced by the <paramref name="converter"/>.</typeparam>
        /// <param name="parquetReader">An initialized Parquet reader for the source data.</param>
        /// <param name="converter">Converter used to transform raw Parquet values to <typeparamref name="T"/>.</param>
        /// <param name="channelNames">Optional list of channel names to include; if <c>null</c> or empty all data fields are processed.</param>
        /// <returns>
        /// A dictionary mapping channel name to a <see cref="List{T}"/> containing converted values accumulated from all row groups.
        /// </returns>
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
        /// Converts values from a Parquet <see cref="Parquet.Data.DataColumn"/> using the provided converter
        /// and appends the converted values to <paramref name="targetList"/>.
        /// </summary>
        /// <typeparam name="T">Type of the converted values.</typeparam>
        /// <param name="columnData">The source column data from a Parquet row group.</param>
        /// <param name="converter">Converter used to transform raw values to <typeparamref name="T"/>.</param>
        /// <param name="targetList">List to append converted values to.</param>
        private static void ConvertAndAddValues<T>(DataColumn columnData, IChannelValueConverter<T> converter, List<T> targetList)
        {
            var data = columnData.Data;

            for (int j = 0; j < data.Length; j++)
            {
                targetList.Add(converter.Convert(data.GetValue(j)));
            }
        }
    }
}
