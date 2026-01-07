#nullable enable
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
        private const string BinaryFileExtension = ".bin";

        public async Task ExecuteAsync(
            IRootFolder root,
            IFileWriter writer,
            bool channelsAsCsv,
            bool deleteProcessedFiles,
            int parallelism,
            string? xDomainFilter = null)
        {
            var studyScalarFiles = new StudyScalarFiles();
            var channelDataFiles = new DomainChannelFiles();
            var channelDataColumns = new ChannelDataColumns();

            var allFiles = await root.GetFilesAsync();
            var filesToWrite = new List<IFile>();
            var binFiles = new List<IFile>();

            foreach (var file in allFiles)
            {
                try
                {
                    TryAddFileToStudyScalarResults.Execute(file, studyScalarFiles);

                    if (TryGetVectorResultsDomain.Execute(file, out var resultsDomain))
                    {
                        // Parquet file path
                        channelDataFiles.Add(resultsDomain);
                    }
                    else if (file.FileName.EndsWith(BinaryFileExtension, StringComparison.InvariantCultureIgnoreCase))
                    {
                        binFiles.Add(file);

                        if (channelsAsCsv && TryGetChannelMetadata.Execute(file, out var channelMetadata))
                        {
                            // legacy bin path
                            channelDataColumns.Add(new CsvColumn(channelMetadata, file));
                        }
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

            if (writer.Writes(ResultsFile.VectorResultsCsv))
            {
                if (channelDataFiles.Any())
                {
                    await WriteChannelDataAsCsv.ExecuteAsync(root, writer, deleteProcessedFiles, parallelism, channelDataFiles, xDomainFilter);
                }
                else
                {
                    await WriteChannelDataAsCsv.ExecuteAsync(root, writer, deleteProcessedFiles, parallelism, channelDataColumns, xDomainFilter);
                }
            }

            if (writer.Writes(ResultsFile.BinaryFiles))
            {
                if (binFiles.Any())
                {
                    await binFiles.ForEachAsync(parallelism, async file =>
                    {
                        await writer.WriteExistingFile(root, file);
                    });
                }
                else
                {
                    await WriteChannelDataAsBinary.ExecuteAsync(root, writer, deleteProcessedFiles, parallelism, channelDataFiles, xDomainFilter);
                }
            }

            await WriteCombinedStudyScalarData.ExecuteAsync(root, writer, studyScalarFiles);
        }
    }
}