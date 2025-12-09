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
        public async Task ExecuteAsync(
            IRootFolder root,
            IFileWriter writer,
            bool channelsAsCsv,
            bool deleteProcessedFiles,
            int parallelism,
            string? simTypeFilter = null,
            string? xDomainFilter = null)
        {
            var studyScalarFiles = new StudyScalarFiles();
            var channelDataFiles = new ChannelDataFiles();
             
            var allFiles = await root.GetFilesAsync();
            var filesToWrite = new List<IFile>();

            // here all .bin files are being red individually
            // we need to change it to the parquet files being read at once instead of for each single column

            foreach (var file in allFiles)
            {
                try
                {
                    TryAddFileToStudyScalarResults.Execute(file, studyScalarFiles);

                    if (channelsAsCsv && TryGetVectorResultsDomain.Execute(file, out var resultsDomain))
                    {
                        channelDataFiles.Add(resultsDomain);
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

            await WriteChannelDataAsCsv.ExecuteAsync(root, writer, deleteProcessedFiles, parallelism, channelDataFiles, simTypeFilter, xDomainFilter);
            await WriteCombinedStudyScalarData.ExecuteAsync(root, writer, studyScalarFiles);
        }
    }
}