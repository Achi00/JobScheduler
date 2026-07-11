using JobScheduler.Core.Errors;

namespace JobScheduler.Core.Storage
{
    internal interface IJobStore
    {
        Task CreateAsync(JobRecord job, CancellationToken cancellationToken);

        Task<JobRecord?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken);

        Task<JobRecord?> TryClaimNextRunnableJobAsync(string workerId, TimeSpan lockDuration, CancellationToken cancellationToken);

        Task<bool> MarkSucceededAsync(Guid jobId, long lockToken, CancellationToken cancellationToken);

        Task<bool> MarkFailedAsync(Guid jobId, long lockToken, JobError error, CancellationToken cancellationToken);

        Task<bool> MarkRetryingAsync(Guid jobId, long lockToken, JobError error, DateTimeOffset nextRunAt, CancellationToken cancellationToken);
    }
}
