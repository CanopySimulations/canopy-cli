namespace Canopy.Cli.Shared.StudyProcessing.StudyScalars
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Text;

    public static class WriteCombinedStudyScalarData
    {
        public static async Task ExecuteAsync(
            IRootFolder root,
            IFileWriter writer,
            StudyScalarFiles studyScalarFiles)
        {
            var content = await GetCombinedStudyScalarDataCsv(root, writer, studyScalarFiles);

            if(content != null)
            {
                var bytes = Encoding.UTF8.GetBytes(content.ToString());
                await writer.WriteNewFile(root, string.Empty, "scalar-results-merged.csv", bytes);
            }
        }

        public static async Task<string> GetCombinedStudyScalarDataCsv(
            IRootFolder root,
            IFileWriter writer,
            StudyScalarFiles studyScalarFiles)
        {
            if (studyScalarFiles.ScalarResults == null || studyScalarFiles.ScalarInputs == null)
            {
                return null;
            }

            var results = new List<ScalarResultItem>();
            var metadata = new List<ScalarMetadataItem>();

            var scalarResultsFile = studyScalarFiles.ScalarResults;
            var scalarMetadataFile = studyScalarFiles.ScalarMetadata;

            var (scalarResults, scalarMetadata, jobIndexColumn) = await GetStudyScalarResultsFileContent(scalarResultsFile, scalarMetadataFile);

            var scalarInputs = await LoadScalarResults(studyScalarFiles.ScalarInputs);
            var scalarInputsMetadata = await LoadScalarInputsMetadata(studyScalarFiles.ScalarInputsMetadata);
            scalarInputsMetadata = scalarInputsMetadata.Skip(1).ToList(); // Skip the column titles row.

            scalarInputs = scalarInputs.Select((v, i) => v.WithMetadata(scalarInputsMetadata?.Count > i ? scalarInputsMetadata[i] : null)).ToList();

            // Now we add the job index, then the scalar inputs, then the scalar results.
            results.Add(jobIndexColumn);
            results.AddRange(scalarInputs);
            results.AddRange(scalarResults);

            metadata.AddRange(scalarInputsMetadata);
            metadata.AddRange(scalarMetadata);

            var csv = new StringBuilder();
            csv.AppendLine(string.Join(",", results.Select(r => r.Metadata?.Description?.WithQuotes() ?? string.Empty)));
            csv.AppendLine(string.Join(",", results.Select(r => (r.Metadata?.FullName ?? r.Name).WithQuotes())));
            csv.AppendLine(string.Join(",", results.Select(r => r.Metadata?.Units?.WithQuotes() ?? string.Empty)));

            for (int resultDataIndex = 0; resultDataIndex < jobIndexColumn.Data.Count; resultDataIndex++)
            {
                var jobIndex = (int)jobIndexColumn.Data[resultDataIndex].ParseJavascriptDouble();
                var lineData = new List<string> {jobIndex.ToJavascriptString()}
                    .Concat(scalarInputs.Select(v => v.Data[jobIndex].WithQuotes()))
                    .Concat(scalarResults.Select(v => v.Data[resultDataIndex].WithQuotes())).ToList();

                csv.AppendLine(string.Join(",", lineData));
            }

            return csv.ToString();
        }

        public static async Task<IReadOnlyList<ScalarResultItem>> GetStudyScalarResultsFromMergedFiles(IFile scalarResultsFile, IFile scalarMetadataFile)
        {
            var (scalarResults, scalarMetadata, jobIndexColumn) = await GetStudyScalarResultsFileContent(scalarResultsFile, scalarMetadataFile);
            return scalarResults;
        }

        private static async Task<(IReadOnlyList<ScalarResultItem> scalarResults, IReadOnlyList<ScalarMetadataItem> scalarMetadata, ScalarResultItem jobIndexColumn)> GetStudyScalarResultsFileContent(IFile scalarResultsFile, IFile scalarMetadataFile)
        {
            var scalarResults = await LoadScalarResults(scalarResultsFile);
            var scalarMetadata = await LoadScalarMetadata(scalarMetadataFile);

            var jobIndexColumn = scalarResults.First();
            scalarResults = scalarResults.Skip(1).ToList(); // Skip the job index column.
            scalarMetadata = scalarMetadata.Skip(1).ToList(); // Skip the column titles row.
            scalarResults = scalarResults.Select((r, i) => r.WithMetadata(scalarMetadata?.Count > i ? scalarMetadata[i] : null)) .ToList();
            return (scalarResults, scalarMetadata, jobIndexColumn);
        }

        private static async Task<IReadOnlyList<ScalarResultItem>> LoadScalarResults(IFile file)
        {
            if(file == null)
            {
                return new List<ScalarResultItem>();
            }

            var content = await file.GetContentAsTextAsync();
            var columns = content.ToCsvColumns();
            var result = columns.Select(c => new ScalarResultItem(
                c[0], 
                c.Skip(1).ToList())).ToList();

            return result;
        }

        private static async Task<IReadOnlyList<ScalarMetadataItem>> LoadScalarMetadata(IFile file)
        {
            if (file == null)
            {
                return new List<ScalarMetadataItem>();
            }

            var content = await file.GetContentAsTextAsync();
            var rows = content.ToCsvRows();
            var result = rows.Select(r => new ScalarMetadataItem(
                r[0],
                r[3],
                $"{r[0]}:{r[3]}", 
                r[1],
                r[2])).ToList();

            return result;
        }

        private static async Task<IReadOnlyList<ScalarMetadataItem>> LoadScalarInputsMetadata(IFile file)
        {
            if (file == null)
            {
                return new List<ScalarMetadataItem>();
            }

            var content = await file.GetContentAsTextAsync();
            var rows = content.ToCsvRows();
            var result = rows.Select(r => new ScalarMetadataItem(
                r[0],
                null,
                r[0],
                r[1],
                r[2])).ToList();

            return result;
        }
    }

    public class ScalarResultItem
    {
        public ScalarResultItem(string name, IReadOnlyList<string> data, ScalarMetadataItem metadata = null)
        {
            this.Name = name;
            this.Data = data;
            this.Metadata = metadata;
        }

        public string Name { get; }

        public IReadOnlyList<string> Data { get; }

        public ScalarMetadataItem Metadata { get; }

        public ScalarResultItem WithMetadata(ScalarMetadataItem metadata)
        {
            return new ScalarResultItem(this.Name, this.Data, metadata);
        }
    }

    public class ScalarMetadataItem
    {
        public ScalarMetadataItem(string name, string simType, string fullName, string units, string description)
        {
            this.Name = name;
            this.SimType = simType;
            this.FullName = fullName;
            this.Units = units;
            this.Description = description;
        }

        public string Name { get; }
        public string SimType { get; }
        public string FullName { get; }
        public string Units { get; }
        public string Description { get; }
    }
}