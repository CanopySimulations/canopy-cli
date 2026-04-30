using System;

namespace Canopy.Cli.Executable.Azure
{
    public class RateLimitedProgress<T> : IProgress<T>
    {
        private readonly IProgress<T> inner;
        private readonly TimeSpan rate;

        private DateTimeOffset lastReport = DateTimeOffset.MinValue;

        public RateLimitedProgress(TimeSpan rate, IProgress<T> inner)
        {
            this.inner = inner;
            this.rate = rate;
        }

        public void Report(T value)
        {
            var now = DateTimeOffset.UtcNow;
            if ((now - this.lastReport) < this.rate)
            {
                return;
            }

            this.lastReport = now;
            this.inner.Report(value);
        }
    }
}
