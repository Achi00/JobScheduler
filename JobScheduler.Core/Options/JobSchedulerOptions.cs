namespace JobScheduler.Core.Options
{
    public sealed class JobSchedulerOptions
    {
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);

        public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(5);

        public int DefaultMaxAttempts { get; set; } = 3;

        public int WorkerCount { get; set; } = 1;
    }
}
