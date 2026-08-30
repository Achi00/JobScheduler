using System.Diagnostics;

namespace JobScheduler.Benchmarks
{
    internal sealed class BenchmarkMetrics
    {
        private long _firstClaimTicks;
        private long _lastSucceededTicks;

        public void RecordClaim()
        {
            Interlocked.CompareExchange(
                ref _firstClaimTicks,
                Stopwatch.GetTimestamp(),
                0);
        }

        public void RecordSuccess()
        {
            Interlocked.Exchange(
                ref _lastSucceededTicks,
                Stopwatch.GetTimestamp());
        }

        public TimeSpan Elapsed
        {
            get
            {
                var start = Volatile.Read(ref _firstClaimTicks);
                var end = Volatile.Read(ref _lastSucceededTicks);

                if (start == 0 || end == 0)
                {
                    return TimeSpan.Zero;
                }

                return Stopwatch.GetElapsedTime(start, end);
            }
        }
    }
}
