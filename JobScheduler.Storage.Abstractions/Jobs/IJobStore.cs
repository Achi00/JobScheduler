namespace JobScheduler.Storage.Abstractions.Jobs
{
    public interface IJobStore
    {
        Task CreateAsync(
            JobRecord job,
            CancellationToken cancellationToken);

        Task<JobRecord?> GetByIdAsync(
            Guid jobId,
            CancellationToken cancellationToken);

        Task<JobRecord?> TryClaimNextRunnableJobAsync(
            string workerId,
            TimeSpan lockDuration,
            CancellationToken cancellationToken);

        Task<JobStateChangeResult> MarkSucceededAsync(
            Guid jobId,
            long lockToken,
            CancellationToken cancellationToken);

        Task<JobStateChangeResult> MarkFailedAsync(
            Guid jobId,
            long lockToken,
            JobError error,
            CancellationToken cancellationToken);

        Task<JobStateChangeResult> MarkRetryingAsync(
            Guid jobId,
            long lockToken,
            JobError error,
            DateTimeOffset nextRunAt,
            CancellationToken cancellationToken);

        Task<int> RecoverExpiredJobsAsync(
            int batchSize,
            TimeSpan recoveryDelay,
            CancellationToken cancellationToken);
    }
}
