namespace JobScheduler.Abstractions.Enums
{
    public enum JobStatus
    {
        // first attempt not run yet
        Enqueued = 1,
        Processing = 2,
        Succeeded = 3,
        // exhausted all retries
        Failed = 4,
        // one off job, deliberately delayed (executed based on NextRunAt in future)
        Scheduled = 5,
        // failed at leadt once, waiting backoff, will be retried
        Retrying = 6,
        Cancelled = 7
    }
}
