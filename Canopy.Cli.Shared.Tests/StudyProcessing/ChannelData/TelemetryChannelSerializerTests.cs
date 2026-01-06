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

            using (var parquetWriter = await ParquetWriter.CreateAsync(schema, memoryStream))
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
                        await groupWriter.WriteColumnAsync(dataColumn);
                    }
                    else if (firstValue is double)
                    {
                        var dataColumn = new DataColumn(
                            field as DataField,
                            column.Value.Cast<double>().ToArray());
                        await groupWriter.WriteColumnAsync(dataColumn);
                    }
                    else if (firstValue is int)
                    {
                        var dataColumn = new DataColumn(
                            field as DataField,
                            column.Value.Cast<int>().ToArray());
                        await groupWriter.WriteColumnAsync(dataColumn);
                    }
                    else if (firstValue is long)
                    {
                        var dataColumn = new DataColumn(
                            field as DataField,
                            column.Value.Cast<long>().ToArray());
                        await groupWriter.WriteColumnAsync(dataColumn);
                    }
                }
            }

            return memoryStream.ToArray();
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithSingleFloatChannel_ShouldReturnCorrectBinaryData()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f, 3.0f } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, new FloatChannelValueConverter());

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey("Channel1"));
            
            var floatData = result["Channel1"];           
            CollectionAssert.AreEqual(new[] { 1.0f, 2.0f, 3.0f }, floatData);
        }

        public async Task ConvertChannelsFromStreamAsync_WithSingleFloatChannel_ShouldReturnCorrectBinaryData()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "Channel1", new List<object> { 1.0f, 2.0f, 3.0f } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);
            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, new FloatChannelValueConverter());

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.ContainsKey("Channel1"));

            var floatData = result["Channel1"];
            CollectionAssert.AreEqual(new[] { 1.0f, 2.0f, 3.0f }, floatData);
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
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, new FloatChannelValueConverter());

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
            var parquetBytes = await CreateTestParquetFileAsync(testData);
            var requestedChannels = new List<string> { "Channel1", "Channel3" };

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, new FloatChannelValueConverter(), requestedChannels);

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
            var parquetBytes = await CreateTestParquetFileAsync(testData);
            var requestedChannels = new List<string> { "Channel1", "NonExistent" };

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, new FloatChannelValueConverter(), requestedChannels);

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
            var parquetBytes = await CreateTestParquetFileAsync(testData);
            var requestedChannels = new List<string>();

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, new FloatChannelValueConverter(), requestedChannels);

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
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, new FloatChannelValueConverter(), null);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithDoubleValuesAndFloatConverter_ShouldConvertToFloat()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "DoubleChannel", new List<object> { 1.5, 2.5, 3.5 } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, new FloatChannelValueConverter());

            Assert.AreEqual(1, result.Count);
            var floats = result["DoubleChannel"];
            
            Assert.AreEqual(1.5f, floats[0], 0.0001f);
            Assert.AreEqual(2.5f, floats[1], 0.0001f);
            Assert.AreEqual(3.5f, floats[2], 0.0001f);
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithIntValuesAndFloatConverter_ShouldConvertToFloat()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "IntChannel", new List<object> { 1, 2, 3 } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, new FloatChannelValueConverter());

            Assert.AreEqual(1, result.Count);
            var floats = result["IntChannel"];
            CollectionAssert.AreEqual(new[] { 1.0f, 2.0f, 3.0f }, floats);
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithLongValuesAndFloatConverter_ShouldConvertToFloat()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "LongChannel", new List<object> { 1L, 2L, 3L } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, new FloatChannelValueConverter());

            Assert.AreEqual(1, result.Count);
            var floats = result["LongChannel"];
            CollectionAssert.AreEqual(new[] { 1.0f, 2.0f, 3.0f }, floats);
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithMixedDataTypesAndFloatConverter_ShouldConvertAllToFloat()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "FloatChannel", new List<object> { 1.0f, 2.0f } },
                { "DoubleChannel", new List<object> { 3.0, 4.0 } },
                { "IntChannel", new List<object> { 5, 6 } },
                { "LongChannel", new List<object> { 7L, 8L } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, new FloatChannelValueConverter());

            Assert.AreEqual(4, result.Count);
            foreach (var channel in result)
            {
                Assert.AreEqual(2, channel.Value.Length);
            }
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithMixedDataTypesAndDoubleConverter_ShouldConvertAllToFloat()
        {
            var testData = new Dictionary<string, List<object>>
            {
                { "FloatChannel", new List<object> { 1.0f, 2.0f } },
                { "DoubleChannel", new List<object> { 3.0, 4.0 } },
                { "IntChannel", new List<object> { 5, 6 } },
                { "LongChannel", new List<object> { 7L, 8L } }
            };
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, new DoubleChannelValueConverter());

            Assert.AreEqual(4, result.Count);
            foreach (var channel in result)
            {
                Assert.AreEqual(2, channel.Value.Length);
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
            var parquetBytes = await CreateTestParquetFileAsync(testData);

            var result = await TelemetryChannelSerializer.ConvertChannelsAsync(parquetBytes, new FloatChannelValueConverter());

            Assert.AreEqual(1, result.Count);
            var floats = result["LargeChannel"];
            Assert.AreEqual(10000, floats.Length);
            
            Assert.AreEqual(0.0f, floats[0]);
            Assert.AreEqual(9999.0f, floats[9999]);
        }


        [TestMethod]
        public async Task ConvertChannelsAsync_WithNullParquetBytes_ShouldThrowArgumentNullException()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await TelemetryChannelSerializer.ConvertChannelsAsync((byte[])null!, new FloatChannelValueConverter());
            });
        }

        [TestMethod]
        public async Task ConvertChannelsAsync_WithInvalidParquetData_ShouldThrowIOException()
        {
            var invalidBytes = new byte[] { 1, 2, 3, 4, 5 };
            
            var exception = await Assert.ThrowsExceptionAsync<IOException>(async () =>
            {
                await TelemetryChannelSerializer.ConvertChannelsAsync(invalidBytes, new FloatChannelValueConverter());
            });
            
            Assert.IsTrue(
                exception.Message.Contains("not a Parquet file") || 
                exception.Message.Contains("size too small"),
                $"Unexpected error message: {exception.Message}");
        }

        #region ConvertChannelsStreamAsync Tests

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

            await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
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
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                await foreach (var _ in TelemetryChannelSerializer.ConvertChannelsStreamAsync(null!, new FloatChannelValueConverter(), null, CancellationToken.None))
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