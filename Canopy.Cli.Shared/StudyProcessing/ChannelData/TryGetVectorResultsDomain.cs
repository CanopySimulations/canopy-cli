#nullable enable
using System;

namespace Canopy.Cli.Shared.StudyProcessing.ChannelData
{
    /// <summary>
    /// Parses vector results file names to extract domain and simulation type information.
    /// Expected format: {SimType}_{Domain}_VectorResults.parquet
    /// Example: "DynamicLap_sRun_VectorResults.parquet"
    /// </summary>
    public static class TryGetVectorResultsDomain
    {
        private const string VectorResultsFileEnding = "_VectorResults.parquet";
        private const char DomainSeparator = '_';
        private const int MinimumParts = 2; // SimType and Domain at minimum

        /// <summary>
        /// Attempts to parse a VectorResultsDomain from a file.
        /// </summary>
        /// <param name="file">The file to parse.</param>
        /// <param name="result">The parsed VectorResultsDomain if successful, null otherwise.</param>
        /// <returns>True if parsing succeeded, false otherwise.</returns>
        public static bool Execute(IFile file, out VectorResultsDomain? result)
        {
            result = null;

            var fileName = file.FileName;
            if (!fileName.EndsWith(VectorResultsFileEnding, StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            // Remove the "_VectorResults.parquet" suffix
            var nameWithoutSuffix = fileName[..^VectorResultsFileEnding.Length];

            var parts = nameWithoutSuffix.Split(DomainSeparator);
            if (parts.Length < MinimumParts)
            {
                return false;
            }

            var simType = parts[0];
            if (string.IsNullOrWhiteSpace(simType))
            {
                return false;
            }

            // Domain is everything after the first separator
            var domain = nameWithoutSuffix[(simType.Length + 1)..];
            if (string.IsNullOrWhiteSpace(domain))
            {
                return false;
            }

            result = new VectorResultsDomain(domain, simType, file);
            return true;
        }
    }
}