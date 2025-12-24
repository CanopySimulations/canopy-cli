#nullable enable
using Parquet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    /// <summary>
    /// Utility for converting Parquet channel data to binary format.
    /// </summary>
    public static class ParquetChannelConverter
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

            var channelDataDict = await ExtractChannelsAsync(parquetBytes, channelNames);
            var result = new Dictionary<string, byte[]>(channelDataDict.Count);

            foreach (var kvp in channelDataDict)
            {
                result[kvp.Key] = ConvertFloatsToBinary(kvp.Value);
            }

            return result;
        }

        /// <summary>
        /// Extracts channels from Parquet bytes.
        /// </summary>
        private static async Task<Dictionary<string, List<float>>> ExtractChannelsAsync(byte[] parquetBytes, IReadOnlyList<string>? channelNames)
        {
            using var memoryStream = new MemoryStream(parquetBytes);
            using var parquetReader = await ParquetReader.CreateAsync(memoryStream);

            var channelNamesSet = channelNames != null && channelNames.Count > 0
                ? new HashSet<string>(channelNames)
                : null;

            var channelData = new Dictionary<string, List<float>>();

            for (int i = 0; i < parquetReader.RowGroupCount; i++)
            {
                using var rowGroupReader = parquetReader.OpenRowGroupReader(i);

                var dataFields = channelNamesSet != null
                    ? parquetReader.Schema.GetDataFields().Where(f => channelNamesSet.Contains(f.Name))
                    : parquetReader.Schema.GetDataFields();

                foreach (var field in dataFields)
                {
                    var columnData = await rowGroupReader.ReadColumnAsync(field);

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
