using System.Threading.Tasks;

namespace Canopy.Cli.Shared
{
    public interface IFile
    {
        string FileName { get; }

        string FullPath { get; }

        string RelativePathToFile { get; }

        Task<byte[]> GetContentAsBytesAsync();

        Task<string> GetContentAsTextAsync();
    }
    
}
