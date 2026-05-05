using System;

namespace Canopy.Cli.Executable.Azure
{
    public class RateLimitedProgress<T>
    {
        private readonly Action<T> callback;
        private readonly TimeSpan rate;

        private DateTimeOffset lastReport = DateTimeOffset.MinValue;

        public RateLimitedProgress(TimeSpan rate, Action<T> callback)
        {
            this.callback = callback;
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
            this.callback(value);
        }
    }
}
