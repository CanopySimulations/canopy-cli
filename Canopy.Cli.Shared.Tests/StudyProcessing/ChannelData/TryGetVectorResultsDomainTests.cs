#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    [TestClass]
    public class TryGetVectorResultsDomainTests
    {
        private class MockFile : IFile
        {
            public MockFile(string fileName, string fullPath = "", string relativePath = "")
            {
                FileName = fileName;
                FullPath = fullPath;
                RelativePathToFile = relativePath;
            }

            public string FileName { get; }
            public string FullPath { get; }
            public string RelativePathToFile { get; }

            public Task<byte[]> GetContentAsBytesAsync() => Task.FromResult(new byte[0]);
            public Task<string> GetContentAsTextAsync() => Task.FromResult(string.Empty);
        }

        [TestMethod]
        public void Execute_WithValidFileName_ShouldParseCorrectly()
        {
            var file = new MockFile("DynamicLap_sRun_VectorResults.parquet");

            var success = TryGetVectorResultsDomain.Execute(file, out var result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual("DynamicLap", result.SimType);
            Assert.AreEqual("sRun", result.Domain);
            Assert.AreSame(file, result.File);
        }

        [TestMethod]
        public void Execute_WithDomainContainingUnderscores_ShouldParseCorrectly()
        {
            var file = new MockFile("DynamicLap_RacingLine_sLap_VectorResults.parquet");

            var success = TryGetVectorResultsDomain.Execute(file, out var result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual("DynamicLap", result.SimType);
            Assert.AreEqual("RacingLine_sLap", result.Domain);
        }

        [TestMethod]
        public void Execute_WithSimTypeContainingUnderscores_ShouldTakeFirstPartOnly()
        {
            var file = new MockFile("Dynamic_Lap_sRun_VectorResults.parquet");

            var success = TryGetVectorResultsDomain.Execute(file, out var result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual("Dynamic", result.SimType);
            Assert.AreEqual("Lap_sRun", result.Domain);
        }

        [TestMethod]
        public void Execute_WithLongDomainName_ShouldParseCorrectly()
        {
            var file = new MockFile("SimType_Very_Long_Domain_Name_With_Many_Parts_VectorResults.parquet");

            var success = TryGetVectorResultsDomain.Execute(file, out var result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual("SimType", result.SimType);
            Assert.AreEqual("Very_Long_Domain_Name_With_Many_Parts", result.Domain);
        }

        [TestMethod]
        public void Execute_WithCaseInsensitiveSuffix_ShouldParseCorrectly()
        {
            var file = new MockFile("DynamicLap_sRun_VECTORRESULTS.PARQUET");

            var success = TryGetVectorResultsDomain.Execute(file, out var result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual("DynamicLap", result.SimType);
            Assert.AreEqual("sRun", result.Domain);
        }

        [TestMethod]
        public void Execute_WithIncorrectSuffix_ShouldReturnFalse()
        {
            var file = new MockFile("DynamicLap_sRun_VectorResults.csv");

            var success = TryGetVectorResultsDomain.Execute(file, out var result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Execute_WithNoSuffix_ShouldReturnFalse()
        {
            var file = new MockFile("DynamicLap_sRun");

            var success = TryGetVectorResultsDomain.Execute(file, out var result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Execute_WithOnlySimType_ShouldReturnFalse()
        {
            var file = new MockFile("DynamicLap_VectorResults.parquet");

            var success = TryGetVectorResultsDomain.Execute(file, out var result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Execute_WithNoUnderscores_ShouldReturnFalse()
        {
            var file = new MockFile("DynamicLapVectorResults.parquet");

            var success = TryGetVectorResultsDomain.Execute(file, out var result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Execute_WithEmptySimType_ShouldReturnFalse()
        {
            var file = new MockFile("_sRun_VectorResults.parquet");

            var success = TryGetVectorResultsDomain.Execute(file, out var result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Execute_WithWhitespaceDomain_ShouldReturnFalse()
        {
            var file = new MockFile("DynamicLap_ _VectorResults.parquet");

            var success = TryGetVectorResultsDomain.Execute(file, out var result);

            Assert.IsFalse(success);
            Assert.IsNull(result);
        }

        [TestMethod]
        public void Execute_WithSpecialCharactersInSimType_ShouldParseCorrectly()
        {
            var file = new MockFile("Sim-Type_Domain_VectorResults.parquet");

            var success = TryGetVectorResultsDomain.Execute(file, out var result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual("Sim-Type", result.SimType);
            Assert.AreEqual("Domain", result.Domain);
        }

        [TestMethod]
        public void Execute_WithSpecialCharactersInDomain_ShouldParseCorrectly()
        {
            var file = new MockFile("SimType_Domain-Name_VectorResults.parquet");

            var success = TryGetVectorResultsDomain.Execute(file, out var result);

            Assert.IsTrue(success);
            Assert.IsNotNull(result);
            Assert.AreEqual("SimType", result.SimType);
            Assert.AreEqual("Domain-Name", result.Domain);
        }
    }
}
