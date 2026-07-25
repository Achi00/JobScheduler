namespace JobScheduler.Core.Options
{
    // client configurable job options
    public sealed class JobSchedulerOptions
    {
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);

        public TimeSpan LockDuration { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan LeaseRecoveryInterval { get; set; } = TimeSpan.FromMinutes(1);

        public int LeaseRecoveryBatchSize { get; set; } = 100;

        public int DefaultMaxAttempts { get; set; } = 3;

        public int WorkerCount { get; set; } = 1;
    }
}
