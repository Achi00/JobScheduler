namespace JobScheduler.Abstractions.Enums
{
    public enum JobStatus
    {
        Enqueued = 1,
        Processing = 2,
        Succeeded = 3,
        Failed = 4,
        Scheduled = 5,
        Deleted = 6
    }
}
