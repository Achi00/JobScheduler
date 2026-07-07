namespace JobScheduler.Core.Storage
{
    internal interface IJobStore
    {
        Task<JobRecord?> TryClaimNextRunnableJobAsync(string workerId, TimeSpan lockDuration, CancellationToken cancellationToken);
        Task CreateAsync(JobRecord job, CancellationToken cancellationToken);
        Task MarkSucceededAsync(Guid jobId, long lockToken, CancellationToken cancellationToken);
        Task MarkFailedAsync(Guid jobId, long lockToken, string error, CancellationToken cancellationToken);
        Task MarkRetryingAsync(Guid jobId, long lockToken, string error, DateTimeOffset nextRunAt, CancellationToken cancellationToken);
    }
}
