#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Parquet;
using Parquet.Data;
using Parquet.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    [TestClass]
    public class TelemetryChannelSerializerTests
    {
        private async Task<byte[]> CreateTestParquetFileAsync(Dictionary<string, List<object>> columns)
        {
            await using var memoryStream = new MemoryStream();

            var fields = columns.Select(kv =>
            {
                var sample = kv.Value.FirstOrDefault();
                return sample switch
                {
                    float => (Field)new DataField<float>(kv.Key),
                    double => new DataField<double>(kv.Key),
                    _ => new DataField<float>(kv.Key)
                };
            }).ToList();

            if (fields.Count == 0)
            {
                return memoryStream.ToArray();
            }

            var schema = new ParquetSchema(fields);

            using (var parquetWriter = await ParquetWriter.CreateAsync(schema, memoryStream))
            using (var groupWriter = parquetWriter.CreateRowGroup())
            {
                foreach (var kv in columns)
                {
                    var field = (DataField)fields.First(f => f.Name == kv.Key);
                    var sample = kv.Value.FirstOrDefault();

                    switch (sample)
                    {
                        case float:
                            await groupWriter.WriteColumnAsync(new DataColumn(field, kv.Value.Cast<float>().ToArray()));
                            break;
                        case double:
                            await groupWriter.WriteColumnAsync(new DataColumn(field, kv.Value.Cast<double>().ToArray()));
                            break;                     
                        default:
                            // Fallback: try to convert numeric-like values to float
                            var fallback = kv.Value.Select(v => Convert.ToSingle(v)).ToArray();
                            await groupWriter.WriteColumnAsync(new DataColumn(field, fallback));
                            break;
                    }
                }
            }

            return memoryStream.ToArray();
        }
        #region ConvertChannelsStreamAsync Tests

        [TestMethod]
        public async Task ConvertChannelsFromStreamAsync_WithSingleFloatChannel_ShouldReturnCorrectBinaryData()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f, 3.0f } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            using var ms = new MemoryStream(parquetBytes);
            var results = new List<(string Name, float[] Data)>();
            var typeConverter = new FloatChannelValueConverter();

            await foreach (var item in TelemetryChannelSerializer.ConvertChannelsStreamAsync(ms, typeConverter, null, CancellationToken.None))
            {
                results.Add((item.Name, item.Data));
            }

            Assert.AreEqual(1, results.Count);
            Assert.IsTrue(results.Any(r => r.Name == "Channel1"));

            var floatData = results.First(r => r.Name == "Channel1").Data;
            CollectionAssert.AreEqual(new[] { 1.0f, 2.0f, 3.0f }, floatData);
        }

        private async Task<List<(string Name, float[] Data)>> ConvertChannelsStreamToList(
            byte[] parquetBytes,
            IReadOnlyList<string>? channelNames = null,
            CancellationToken cancellationToken = default)
        {
            var typeConverter = new FloatChannelValueConverter();
            var results = new List<(string Name, float[] Data)>();
            await foreach (var item in TelemetryChannelSerializer.ConvertChannelsStreamAsync(
                parquetBytes, typeConverter, channelNames, cancellationToken))
            {
                results.Add((item.Name, item.Data));
            }
            return results;
        }

        [TestMethod]
        public async Task ConvertChannelsStreamAsync_WithSingleChannel_ShouldYieldCorrectData()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f, 3.0f } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var results = await ConvertChannelsStreamToList(parquetBytes);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual("Channel1", results[0].Name);

            var floats = results[0].Data;
            CollectionAssert.AreEqual(new[] { 1.0f, 2.0f, 3.0f }, floats);
        }

        [TestMethod]
        public async Task ConvertChannelsStreamAsync_WithMultipleChannels_ShouldYieldAllChannels()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f } },
                { "Channel2", new List<object> { 3.0f, 4.0f } },
                { "Channel3", new List<object> { 5.0f, 6.0f } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var results = await ConvertChannelsStreamToList(parquetBytes);

            Assert.AreEqual(3, results.Count);
            var channelNames = results.Select(r => r.Name).ToList();
            CollectionAssert.Contains(channelNames, "Channel1");
            CollectionAssert.Contains(channelNames, "Channel2");
            CollectionAssert.Contains(channelNames, "Channel3");
        }

        [TestMethod]
        public async Task ConvertChannelsStreamAsync_WithSpecificChannelNames_ShouldYieldOnlyRequestedChannels()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f } },
                { "Channel2", new List<object> { 3.0f, 4.0f } },
                { "Channel3", new List<object> { 5.0f, 6.0f } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);
            var requestedChannels = new List<string> { "Channel1", "Channel3" };

            var results = await ConvertChannelsStreamToList(parquetBytes, requestedChannels);

            Assert.AreEqual(2, results.Count);
            var channelNames = results.Select(r => r.Name).ToList();
            CollectionAssert.Contains(channelNames, "Channel1");
            CollectionAssert.DoesNotContain(channelNames, "Channel2");
            CollectionAssert.Contains(channelNames, "Channel3");
        }

        [TestMethod]
        public async Task ConvertChannelsStreamAsync_WithEmptyChannelList_ShouldYieldAllChannels()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f } },
                { "Channel2", new List<object> { 3.0f, 4.0f } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);
            var requestedChannels = new List<string>();

            var results = await ConvertChannelsStreamToList(parquetBytes, requestedChannels);

            Assert.AreEqual(2, results.Count);
        }

        [TestMethod]
        public async Task ConvertChannelsStreamAsync_WithNullChannelList_ShouldYieldAllChannels()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f } },
                { "Channel2", new List<object> { 3.0f, 4.0f } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var results = await ConvertChannelsStreamToList(parquetBytes);

            Assert.AreEqual(2, results.Count);
        }

        [TestMethod]
        public async Task ConvertChannelsStreamAsync_WithMixedDataTypes_ShouldConvertAllToFloat()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "FloatChannel", new List<object> { 1.0f, 2.0f } },
                { "DoubleChannel", new List<object> { 3.0, 4.0 } },
                { "IntChannel", new List<object> { 5, 6 } },
                { "LongChannel", new List<object> { 7L, 8L } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var results = await ConvertChannelsStreamToList(parquetBytes);

            Assert.AreEqual(4, results.Count);
            foreach (var result in results)
            {
                Assert.AreEqual(2, result.Data.Length);
            }
        } 

        [TestMethod]
        public async Task ConvertChannelsStreamAsync_WithCancelledToken_ShouldThrowImmediately()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in TelemetryChannelSerializer.ConvertChannelsStreamAsync(parquetBytes, new FloatChannelValueConverter(), null, cts.Token))
                {
                    Assert.Fail("Should not yield any items with cancelled token");
                }
            });
        }

        [TestMethod]
        public async Task ConvertChannelsStreamAsync_WithNullStream_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            {
                await foreach (var _ in TelemetryChannelSerializer.ConvertChannelsStreamAsync((byte[]?)null!, new FloatChannelValueConverter(), null, CancellationToken.None))
                {
                    Assert.Fail("Should not yield any items");
                }
            });
        }

        [TestMethod]
        public async Task ConvertChannelsStreamAsync_WithLargeDataset_ShouldHandleCorrectly()
        {
            var largeDataset = Enumerable.Range(0, 10000).Select(i => (object)(float)i).ToList();
            var testData = new Dictionary<string, List<object>>
            {
                { "LargeChannel", largeDataset }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var results = await ConvertChannelsStreamToList(parquetBytes);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(10000, results[0].Data.Length);

            var floats = results[0].Data;
            Assert.AreEqual(0.0f, floats[0]);
            Assert.AreEqual(9999.0f, floats[9999]);
        }

        [TestMethod]
        public async Task ConvertChannelsStreamAsync_StreamingBehavior_ShouldYieldOneAtATime()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f } },
                { "Channel2", new List<object> { 3.0f, 4.0f } },
                { "Channel3", new List<object> { 5.0f, 6.0f } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var yieldCount = 0;
            await foreach (var item in TelemetryChannelSerializer.ConvertChannelsStreamAsync(parquetBytes, new FloatChannelValueConverter(), null, CancellationToken.None))
            {
                yieldCount++;
                Assert.IsNotNull(item.Name);
                Assert.IsNotNull(item.Data);
                Assert.IsTrue(item.Data.Length > 0);
            }

            Assert.AreEqual(3, yieldCount);
        }

        #endregion
    }
}