namespace Canopy.Cli.Shared.StudyProcessing
{
    /// <summary>
    /// Types of files that can be generated during study processing.
    /// </summary>
    public enum ResultsFile
    {
        /// <summary>
        /// CSV files containing vector results.
        /// </summary>
        VectorResultsCsv,

        /// <summary>
        /// Binary .bin files containing channel data.
        /// </summary>
        BinaryFiles,

        /// <summary>
        /// Merged scalar results CSV file.
        /// </summary>
        MergedScalarResults,
    }
}
