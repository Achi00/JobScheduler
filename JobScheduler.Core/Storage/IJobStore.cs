namespace JobScheduler.Core.Storage
{
    internal interface IJobStore
    {
        Task CreateAsync(JobRecord job, CancellationToken cancellationToken);
        Task<JobRecord?> GetNextRunnableJobAsync(CancellationToken cancellationToken);
        Task MarkProcessingAsync(Guid jobId, CancellationToken cancellationToken);
        Task MarkSucceededAsync(Guid jobId, CancellationToken cancellationToken);
        Task MarkFailedAsync(Guid jobId, string error, CancellationToken cancellationToken);
    }
}
