namespace Canopy.Cli.Shared.StudyProcessing.StudyScalars
{
    public static class TryAddFileToStudyScalarResults
    {
        public static bool Execute(IFile file, StudyScalarFiles studyScalarFiles)
        {
            switch (file.FileName)
            {
                case Constants.StudyScalarResultsFileName:
                    studyScalarFiles.ScalarResults = file;
                    return true;

                case Constants.StudyScalarMetadataFileName:
                    studyScalarFiles.ScalarMetadata = file;
                    return true;

                case Constants.StudyScalarInputsFileName:
                    studyScalarFiles.ScalarInputs = file;
                    return true;

                case Constants.StudyScalarInputsMetadataFileName:
                    studyScalarFiles.ScalarInputsMetadata = file;
                    return true;
            }

            return false;
        }
    }
}