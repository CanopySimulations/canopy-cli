namespace Canopy.Cli.Shared
{
    public static class Constants
    {
        public const string StudyScalarResultsFileName = "scalar-results.csv";
        public const string StudyScalarMetadataFileName = "scalar-metadata.csv";
        public const string StudyScalarInputsFileName = "scalar-inputs.csv";
        public const string StudyScalarInputsMetadataFileName = "scalar-inputs-metadata.csv";
        public const string StudyScalarCombinedFileName = "scalar-combined.csv";

        public static readonly string[] ScalarResultsFiles = new string[]
        {
            StudyScalarResultsFileName,
            StudyScalarMetadataFileName,
            StudyScalarInputsFileName,
            StudyScalarInputsMetadataFileName,
        };

    }
}