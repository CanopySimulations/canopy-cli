using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Canopy.Cli.Shared.StudyProcessing
{
    using Canopy.Cli.Shared.StudyProcessing.ChannelData;
    using Canopy.Cli.Shared.StudyProcessing.StudyScalars;

    public class ProcessStudyResults : IProcessStudyResults
    {
        public async Task ExecuteAsync(
            IRootFolder root,
            IFileWriter writer,
            bool channelsAsCsv,
            bool deleteProcessedFiles,
            int parallelism,
            string? xDomainFilter = null)
        {
            var studyScalarFiles = new StudyScalarFiles();
            var channelDataColumns = new ChannelDataColumns();

            var allFiles = await root.GetFilesAsync();
            var filesToWrite = new List<IFile>();
            foreach (var file in allFiles)
            {
                try
                {
                    TryAddFileToStudyScalarResults.Execute(file, studyScalarFiles);

                    if (channelsAsCsv && TryGetChannelMetadata.Execute(file, out var channelMetadata))
                    {
                        channelDataColumns.Add(new CsvColumn(channelMetadata, file));
                    }
                    else
                    {
                        filesToWrite.Add(file);
                    }
                }
                catch (Exception t)
                {
                    Console.WriteLine();
                    Console.WriteLine("Failed to process file: " + file);
                    Console.WriteLine(t);

                    if (t is AbortProcessingException)
                    {
                        throw;
                    }
                }
            }

            await filesToWrite.ForEachAsync(parallelism, async file =>
            {
                await writer.WriteExistingFile(root, file);
            });

            await WriteChannelDataAsCsv.ExecuteAsync(root, writer, deleteProcessedFiles, parallelism, channelDataColumns, xDomainFilter);
            await WriteCombinedStudyScalarData.ExecuteAsync(root, writer, studyScalarFiles);
        }
    }
}