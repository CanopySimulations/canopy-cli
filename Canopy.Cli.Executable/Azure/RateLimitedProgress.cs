using System;
using System.Threading;

namespace Canopy.Cli.Executable.Azure
{
    public class RateLimitedProgress<T>
    {
        private readonly Action<T> callback;
        private readonly long rateTicks;

        private long lastReportTicks = DateTimeOffset.MinValue.Ticks;

        public RateLimitedProgress(TimeSpan rate, Action<T> callback)
        {
            this.callback = callback;
            this.rateTicks = rate.Ticks;
        }

        public void Report(T value)
        {
            var nowTicks = DateTimeOffset.UtcNow.Ticks;
            var lastTicks = Interlocked.Read(ref this.lastReportTicks);
            if (nowTicks - lastTicks < this.rateTicks)
                return;
            if (Interlocked.CompareExchange(ref this.lastReportTicks, nowTicks, lastTicks) != lastTicks)
                return;
            this.callback(value);
        }
    }
}
