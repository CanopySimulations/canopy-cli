#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    [TestClass]
    public class TelemetryChannelSerializerTests
    {
        private byte[] CreateTestParquetFile(Dictionary<string, List<object>> columns)
        {
            using var memoryStream = new MemoryStream();
            
            var fields = new List<Field>();
            foreach (var column in columns)
            {
                var firstValue = column.Value.FirstOrDefault();
                if (firstValue is float)
                    fields.Add(new DataField<float>(column.Key));
                else if (firstValue is double)
                    fields.Add(new DataField<double>(column.Key));
                else if (firstValue is int)
                    fields.Add(new DataField<int>(column.Key));
                else if (firstValue is long)
                    fields.Add(new DataField<long>(column.Key));
                else
                    fields.Add(new DataField<float>(column.Key));
            }

            if (fields.Count == 0)
            {
                return memoryStream.ToArray();
            }

            var schema = new ParquetSchema(fields);

            using (var parquetWriter = ParquetWriter.CreateAsync(schema, memoryStream).Result)
            {
                using var groupWriter = parquetWriter.CreateRowGroup();
                
                foreach (var column in columns)
                {
                    var field = fields.First(f => f.Name == column.Key);
                    var firstValue = column.Value.FirstOrDefault();
                    
                    if (firstValue is float)
                    {
                        var dataColumn = new DataColumn(
                            field as DataField,
                            column.Value.Cast<float>().ToArray());
                        groupWriter.WriteColumnAsync(dataColumn).Wait();
                    }
                    else if (firstValue is double)
                    {
                        var dataColumn = new DataColumn(
                            field as DataField,
                            column.Value.Cast<double>().ToArray());
                        groupWriter.WriteColumnAsync(dataColumn).Wait();
                    }
                    else if (firstValue is int)
                    {
                        var dataColumn = new DataColumn(
                            field as DataField,
                            column.Value.Cast<int>().ToArray());
                        groupWriter.WriteColumnAsync(dataColumn).Wait();
                    }
                    else if (firstValue is long)
                    {
                        var dataColumn = new DataColumn(
                            field as DataField,
                            column.Value.Cast<long>().ToArray());
                        groupWriter.WriteColumnAsync(dataColumn).Wait();
                    }
                }
            }

            return memoryStream.ToArray();
        }

        private static float[] BytesToFloatArray(byte[] bytes)
        {
            var floats = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            return floats;
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithSingleFloatChannel_ShouldReturnCorrectBinaryData()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f, 3.0f } }
            };
            var parquetBytes = CreateTestParquetFile(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey("Channel1"));
            
            var binaryData = result["Channel1"];
            Assert.AreEqual(3 * sizeof(float), binaryData.Length);
            
            var floats = BytesToFloatArray(binaryData);
            CollectionAssert.AreEqual(new[] { 1.0f, 2.0f, 3.0f }, floats);
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithMultipleChannels_ShouldReturnAllChannels()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f } },
                { "Channel2", new List<object> { 3.0f, 4.0f } },
                { "Channel3", new List<object> { 5.0f, 6.0f } }
            };
            var parquetBytes = CreateTestParquetFile(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.ContainsKey("Channel1"));
            Assert.IsTrue(result.ContainsKey("Channel2"));
            Assert.IsTrue(result.ContainsKey("Channel3"));
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithSpecificChannelNames_ShouldReturnOnlyRequestedChannels()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f } },
                { "Channel2", new List<object> { 3.0f, 4.0f } },
                { "Channel3", new List<object> { 5.0f, 6.0f } }
            };
            var parquetBytes = CreateTestParquetFile(testData);
            var requestedChannels = new List<string> { "Channel1", "Channel3" };

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, requestedChannels);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.ContainsKey("Channel1"));
            Assert.IsFalse(result.ContainsKey("Channel2"));
            Assert.IsTrue(result.ContainsKey("Channel3"));
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithNonExistentChannel_ShouldNotIncludeIt()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f } }
            };
            var parquetBytes = CreateTestParquetFile(testData);
            var requestedChannels = new List<string> { "Channel1", "NonExistent" };

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, requestedChannels);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey("Channel1"));
            Assert.IsFalse(result.ContainsKey("NonExistent"));
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithEmptyChannelList_ShouldReturnAllChannels()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f } },
                { "Channel2", new List<object> { 3.0f, 4.0f } }
            };
            var parquetBytes = CreateTestParquetFile(testData);
            var requestedChannels = new List<string>();

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, requestedChannels);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithNullChannelList_ShouldReturnAllChannels()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f } },
                { "Channel2", new List<object> { 3.0f, 4.0f } }
            };
            var parquetBytes = CreateTestParquetFile(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, null);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithDoubleValues_ShouldConvertToFloat()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "DoubleChannel", new List<object> { 1.5, 2.5, 3.5 } }
            };
            var parquetBytes = CreateTestParquetFile(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes);

            Assert.AreEqual(1, result.Count);
            var floats = BytesToFloatArray(result["DoubleChannel"]);
            
            Assert.AreEqual(1.5f, floats[0], 0.0001f);
            Assert.AreEqual(2.5f, floats[1], 0.0001f);
            Assert.AreEqual(3.5f, floats[2], 0.0001f);
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithIntValues_ShouldConvertToFloat()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "IntChannel", new List<object> { 1, 2, 3 } }
            };
            var parquetBytes = CreateTestParquetFile(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes);

            Assert.AreEqual(1, result.Count);
            var floats = BytesToFloatArray(result["IntChannel"]);
            CollectionAssert.AreEqual(new[] { 1.0f, 2.0f, 3.0f }, floats);
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithLongValues_ShouldConvertToFloat()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "LongChannel", new List<object> { 1L, 2L, 3L } }
            };
            var parquetBytes = CreateTestParquetFile(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes);

            Assert.AreEqual(1, result.Count);
            var floats = BytesToFloatArray(result["LongChannel"]);
            CollectionAssert.AreEqual(new[] { 1.0f, 2.0f, 3.0f }, floats);
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithMixedDataTypes_ShouldConvertAllToFloat()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "FloatChannel", new List<object> { 1.0f, 2.0f } },
                { "DoubleChannel", new List<object> { 3.0, 4.0 } },
                { "IntChannel", new List<object> { 5, 6 } },
                { "LongChannel", new List<object> { 7L, 8L } }
            };
            var parquetBytes = CreateTestParquetFile(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes);

            Assert.AreEqual(4, result.Count);
            foreach (var channel in result)
            {
                Assert.AreEqual(2 * sizeof(float), channel.Value.Length);
            }
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithLargeDataset_ShouldHandleCorrectly()
        {
            var largeDataset = Enumerable.Range(0, 10000).Select(i => (object)(float)i).ToList();
            var testData = new Dictionary<string, List<object>>
            {
                { "LargeChannel", largeDataset }
            };
            var parquetBytes = CreateTestParquetFile(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes);

            Assert.AreEqual(1, result.Count);
            var binaryData = result["LargeChannel"];
            Assert.AreEqual(10000 * sizeof(float), binaryData.Length);
            
            var floats = BytesToFloatArray(binaryData);
            Assert.AreEqual(0.0f, floats[0]);
            Assert.AreEqual(9999.0f, floats[9999]);
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithNullParquetBytes_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await TelemetryChannelSerializer.ConvertChannelsAsync(null!);
            });
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithInvalidParquetData_ShouldThrowIOException()
        {
            var invalidBytes = new byte[] { 1, 2, 3, 4, 5 };
            
            var exception = await Assert.ThrowsExceptionAsync<IOException>(async () =>
            {
                await TelemetryChannelSerializer.ConvertChannelsAsync(invalidBytes);
            });
            
            Assert.IsTrue(
                exception.Message.Contains("not a Parquet file") || 
                exception.Message.Contains("size too small"),
                $"Unexpected error message: {exception.Message}");
        }
    }
}
