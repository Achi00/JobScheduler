using JobScheduler.Abstractions.Jobs.Enums;

namespace JobScheduler.Core.Storage
{
    // using simple in memory storage and simple locking, for testing
    internal sealed class InMemoryJobStore : IJobStore
    {
        private readonly List<JobRecord> _jobs = [];
        private readonly object _lock = new object();

        public Task CreateAsync(JobRecord job, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                _jobs.Add(job);
            }

            return Task.CompletedTask;
        }

        public Task<JobRecord?> GetNextRunnableJobAsync(CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;

                var job = _jobs
                .Where(j =>
                    j.Status is JobStatus.Enqueued or JobStatus.Scheduled &&
                    (j.NextRunAt is null || j.NextRunAt <= now))
                .OrderBy(j => j.NextRunAt ?? j.CreatedAt)
                .FirstOrDefault();

                return Task.FromResult(job);
            }
        }

        public Task MarkProcessingAsync(Guid jobId, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var job = GetRequiredJob(jobId);

                job.Status = JobStatus.Processing;
                job.StartedAt = DateTimeOffset.UtcNow;
                job.AttemptCount++;
                job.LastError = null;
            }

            return Task.CompletedTask;
        }

        public Task MarkSucceededAsync(Guid jobId, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var job = GetRequiredJob(jobId);

                job.Status = JobStatus.Succeeded;
                job.CompletedAt = DateTimeOffset.UtcNow;
            }

            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(Guid jobId, string error, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var job = GetRequiredJob(jobId);

                job.Status = JobStatus.Failed;
                job.StartedAt = DateTimeOffset.UtcNow;
                job.AttemptCount++;
                job.LastError = null;
            }

            return Task.CompletedTask;
        }

        private JobRecord GetRequiredJob(Guid jobId)
        {
            return _jobs.FirstOrDefault(j => j.Id == jobId)
                   ?? throw new InvalidOperationException($"Job '{jobId}' was not found.");
        }
    }
}
