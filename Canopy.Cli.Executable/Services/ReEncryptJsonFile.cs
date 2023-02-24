namespace Canopy.Cli.Executable.Services
{
    using System.IO;
    using System.Threading.Tasks;
    using Canopy.Cli.Executable.Services.DownloadMonitoring;
    using Microsoft.Extensions.Logging;
    using System.Threading;
    using Canopy.Cli.Executable.Commands;
    using Xunit;

    public class ReEncryptJsonFile : IReEncryptJsonFile
    {
        private readonly IEnsureAuthenticated ensureAuthenticated;
        private readonly ISimVersionCache simVersionCache;
        private readonly IContainsEncryptedToken containsEncryptedToken;
        private readonly IReEncryptFile reEncryptFile;
        private readonly IFileOperations fileOperations;
        private readonly ILogger<ReEncryptJsonFile> logger;

        public ReEncryptJsonFile(
            IEnsureAuthenticated ensureAuthenticated,
            ISimVersionCache simVersionCache,
            IContainsEncryptedToken containsEncryptedToken,
            IReEncryptFile reEncryptFile,
            IFileOperations fileOperations,
            ILogger<ReEncryptJsonFile> logger)
        {
            this.ensureAuthenticated = ensureAuthenticated;
            this.simVersionCache = simVersionCache;
            this.containsEncryptedToken = containsEncryptedToken;
            this.reEncryptFile = reEncryptFile;
            this.fileOperations = fileOperations;
            this.logger = logger;
        }

        public async Task ExecuteAsync(ReEncryptJsonFileCommand.Parameters parameters)
        {
            var simVersion = await this.simVersionCache.GetOrSet(parameters.SimVersion);

            await this.ensureAuthenticated.ExecuteAsync();

            var cts = CommandUtilities.CreateCommandCancellationTokenSource();

            var filePath = parameters.Target.FullName;
            if (!this.fileOperations.Exists(filePath))
            {
                throw new FileNotFoundException("File not found: " + filePath);
            }

            var content = await this.fileOperations.ReadAllTextAsync(filePath, cts.Token);

            if (this.containsEncryptedToken.Execute(content))
            {
                this.logger.LogInformation("File contains encrypted tokens. Re-encrypting.");
                var newContent = await this.reEncryptFile.ExecuteAsync(content, parameters.DecryptingTenantShortName, simVersion, cts.Token);
                await this.fileOperations.WriteAllTextAsync(filePath, newContent, cts.Token);
            }
            else
            {
                this.logger.LogInformation("File does not contain encrypted token. Skipping.");
            }
        }
    }
}