using System;
using System.Threading.Tasks;

namespace Canopy.Cli.Executable.Services
{
    public interface IWaitForStudy
    {
        Task ExecuteAsync(string tenantId, string studyId, TimeSpan timeout);
    }
}