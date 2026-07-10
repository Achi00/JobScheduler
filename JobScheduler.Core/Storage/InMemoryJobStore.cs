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


        public Task MarkProcessingAsync(Guid jobId, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var job = GetRequiredJob(jobId);

                job.Status = JobStatus.Processing;
                job.StartedAt = DateTimeOffset.UtcNow;
                job.AttemptCount++;
                job.LastErrorMessage = null;
            }

            return Task.CompletedTask;
        }

        public Task MarkSucceededAsync(Guid jobId, long lockToken, CancellationToken ct)
        {
            lock (_lock)
            {
                var job = GetRequiredJob(jobId);

                if (job.LockToken != lockToken)
                {
                    return Task.CompletedTask;
                }

                job.Status = JobStatus.Succeeded;
                job.CompletedAt = DateTimeOffset.UtcNow;
                // release lock of worker
                job.LockedBy = null;
                job.LockedUntil = null;
                job.LastErrorMessage = null;
            }

            return Task.CompletedTask;
        }

        // claim job to specific worker and mark as processing
        public Task<JobRecord?> TryClaimNextRunnableJobAsync(string workerId, TimeSpan lockDuration, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;

                var job = _jobs
                    .Where(j =>
                        (j.Status is JobStatus.Enqueued or JobStatus.Scheduled or JobStatus.Retrying) &&
                        (j.NextRunAt is null || j.NextRunAt <= now))
                    .OrderBy(j => j.NextRunAt ?? j.CreatedAt)
                    .FirstOrDefault();

                if (job is null)
                {
                    return Task.FromResult<JobRecord?>(null);
                }

                job.Status = JobStatus.Processing;
                job.StartedAt = now;
                job.AttemptCount++;
                job.LockedBy = workerId;
                job.LockedUntil = now.Add(lockDuration);
                job.LockToken++;

                // return cloned job with different reference, just for memory storage testing
                return Task.FromResult<JobRecord?>(Clone(job));
            }
        }

        public Task MarkRetryingAsync(Guid jobId, long lockToken, Exception ex, DateTimeOffset nextRunAt, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var job = GetRequiredJob(jobId);

                if (job.LockToken != lockToken)
                {
                    return Task.CompletedTask;
                }

                job.Status = JobStatus.Retrying;
                job.LastErrorMessage = ex.Message;
                job.NextRunAt = nextRunAt;
                // release lock of worker
                job.LockedBy = null;
                job.LockedUntil = null;
            }

            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(Guid jobId, long lockToken, Exception ex, CancellationToken ct)
        {
            lock (_lock)
            {
                var job = GetRequiredJob(jobId);

                job.Status = JobStatus.Failed;
                job.LastErrorMessage = ex.Message;
                job.CompletedAt = DateTimeOffset.UtcNow;

                job.LockedBy = null;
                job.LockedUntil = null;

                job.NextRunAt = null;
            }

            return Task.CompletedTask;
        }

        // using clone only because we work in memory and donw want to use same object reference
        private static JobRecord Clone(JobRecord job)
        {
            return new JobRecord
            {
                Id = job.Id,
                JobType = job.JobType,
                PayloadJson = job.PayloadJson,
                Status = job.Status,
                AttemptCount = job.AttemptCount,
                MaxAttempts = job.MaxAttempts,
                CreatedAt = job.CreatedAt,
                NextRunAt = job.NextRunAt,
                StartedAt = job.StartedAt,
                CompletedAt = job.CompletedAt,
                LastErrorMessage = job.LastErrorMessage,
                LockedBy = job.LockedBy,
                LockedUntil = job.LockedUntil,
                LockToken = job.LockToken
            };
        }

        private JobRecord GetRequiredJob(Guid jobId)
        {
            return _jobs.FirstOrDefault(j => j.Id == jobId)
                   ?? throw new InvalidOperationException($"Job '{jobId}' was not found.");
        }

        public Task<JobRecord?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var job = _jobs.FirstOrDefault(j => j.Id == jobId);

                return Task.FromResult(job is null ? null : Clone(job));
            }
        }
    }
}
