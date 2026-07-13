namespace JobScheduler.Core.Enums
{
    internal enum JobProcessResult
    {
        // worker can sleep
        NoJobAvailable = 1,
        // job was processed
        Succeeded = 2,
        //job failed but will retry
        ScheduledRetry = 3,
        // max attempts reached
        FailedPermanently = 4,
        // worker has stale lock
        LostOwnership = 5,
        // unexpected state issue
        StateChangeFailed = 6
    }
}
