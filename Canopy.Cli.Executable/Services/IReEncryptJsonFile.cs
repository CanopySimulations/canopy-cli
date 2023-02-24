namespace Canopy.Cli.Executable.Services
{
    using System.Threading.Tasks;
    using Canopy.Cli.Executable.Commands;

    public interface IReEncryptJsonFile
    {
        Task ExecuteAsync(ReEncryptJsonFileCommand.Parameters parameters);
    }
}