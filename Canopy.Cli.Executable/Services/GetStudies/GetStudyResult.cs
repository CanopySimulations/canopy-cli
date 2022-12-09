using Canopy.Cli.Shared;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    public record GetStudyResult(string SimVersion)
    {
        public static GetStudyResult Random() => new(
                SingletonRandom.Instance.NextString());
    }
}