namespace JobScheduler.Core.Storage
{
    internal interface IJobStore
    {
        Task CreateAsync(JobRecord job, CancellationToken cancellationToken);

        Task<JobRecord?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

        Task<JobRecord?> TryClaimNextRunnableJobAsync(string workerId, TimeSpan lockDuration, CancellationToken cancellationToken);

        Task MarkSucceededAsync(Guid jobId, long lockToken, CancellationToken cancellationToken);

        Task MarkFailedAsync(Guid jobId, long lockToken, Exception ex, CancellationToken cancellationToken);

        Task MarkRetryingAsync(Guid jobId, long lockToken, Exception ex, DateTimeOffset nextRunAt, CancellationToken cancellationToken);
    }
}
