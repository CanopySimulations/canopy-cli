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
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ArgumentNullException.ThrowIfNull(parquetStream);
            ArgumentNullException.ThrowIfNull(converter);

            using var parquetReader = await ParquetReader.CreateAsync(parquetStream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var channelNamesSet = channelNames != null && channelNames.Count > 0
            ? new HashSet<string>(channelNames)
            : null;

            var dataFields = channelNamesSet != null
                    ? parquetReader.Schema.DataFields.Where(f => channelNamesSet.Contains(f.Name))
                    : parquetReader.Schema.DataFields;

            using var rowGroupReader = parquetReader.OpenRowGroupReader(0);

            foreach (var field in dataFields)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var columnData = await rowGroupReader.ReadColumnAsync(field, cancellationToken).ConfigureAwait(false);
                var channelValues = new List<T>(columnData.Data.Length);
                ConvertAndAddValues(columnData, converter, channelValues);

                yield return (field.Name, channelValues.ToArray());
            }
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
