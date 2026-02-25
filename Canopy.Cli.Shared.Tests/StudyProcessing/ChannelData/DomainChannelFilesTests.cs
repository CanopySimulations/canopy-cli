#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    [TestClass]
    public class DomainChannelFilesTests
    {
        private class MockFile : IFile
        {
            public MockFile(string fileName)
            {
                FileName = fileName;
                FullPath = string.Empty;
                RelativePathToFile = string.Empty;
            }

            public string FileName { get; }
            public string FullPath { get; }
            public string RelativePathToFile { get; }

            public Task<byte[]> GetContentAsBytesAsync() => Task.FromResult(Array.Empty<byte>());
            public Task<string> GetContentAsTextAsync() => Task.FromResult(string.Empty);
        }

        [TestMethod]
        public void Add_ShouldGroupDomainsBySameSimType()
        {
            var file = new MockFile("file.parquet");
            var sut = new DomainChannelFiles();

            sut.Add(new VectorResultsDomain("DomainA", "SimA", file));
            sut.Add(new VectorResultsDomain("DomainB", "SimA", file));

            var domains = sut.GetDomains("SimA");

            Assert.AreEqual(2, domains.Count);
            Assert.AreEqual("DomainA", domains[0].Domain);
            Assert.AreEqual("DomainB", domains[1].Domain);
            Assert.AreSame(file, domains[1].File);
        }

        [TestMethod]
        public void Count_ShouldRepresentDistinctSimTypes()
        {
            var sut = new DomainChannelFiles();

            sut.Add(new VectorResultsDomain("DomainA", "SimA", new MockFile("fileA")));
            sut.Add(new VectorResultsDomain("DomainB", "SimB", new MockFile("fileB")));
            sut.Add(new VectorResultsDomain("DomainC", "SimA", new MockFile("fileC")));

            Assert.AreEqual(2, sut.Count());
        }

        [TestMethod]
        public void Any_ShouldReturnTrueWhenEntriesExist()
        {
            var sut = new DomainChannelFiles();
            sut.Add(new VectorResultsDomain("Domain", "Sim", new MockFile("file")));

            Assert.IsTrue(sut.Any());
        }

        [TestMethod]
        public void Any_ShouldReturnFalseWhenEmpty()
        {
            var sut = new DomainChannelFiles();

            Assert.IsFalse(sut.Any());
        }

        [TestMethod]
        public void Count_ShouldBeZeroWhenNoEntries()
        {
            var sut = new DomainChannelFiles();

            Assert.AreEqual(0, sut.Count());
        }
    }
}
