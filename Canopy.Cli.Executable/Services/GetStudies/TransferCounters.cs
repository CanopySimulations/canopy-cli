using System.Threading;

namespace Canopy.Cli.Executable.Services.GetStudies
{
    internal sealed class TransferCounters
    {
        private long completed, failed, skipped;

        public long Completed => Interlocked.Read(ref this.completed);
        public long Failed    => Interlocked.Read(ref this.failed);
        public long Skipped   => Interlocked.Read(ref this.skipped);

        public void IncrementCompleted() => Interlocked.Increment(ref this.completed);
        public void IncrementFailed()    => Interlocked.Increment(ref this.failed);
        public void IncrementSkipped()   => Interlocked.Increment(ref this.skipped);
    }
}
