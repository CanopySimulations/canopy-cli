using System.Collections.Concurrent;
using System.IO;

namespace Canopy.Cli.Executable.Services.DownloadMonitoring
{
    public class AddedDownloadTokensCache : IAddedDownloadTokensCache
    {
        private readonly ConcurrentDictionary<string, bool> cache = new();

        public bool TryAdd(string filePath)
        {
            return this.cache.TryAdd(new FileInfo(filePath).FullName, true);
        }

        public bool TryRemove(string filePath)
        {
            return this.cache.TryRemove(new FileInfo(filePath).FullName, out var value);
        }
    }
}