using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.Storage.Abstractions.Jobs;

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

        public Task<JobStateChangeResult> MarkSucceededAsync(Guid jobId, long lockToken, CancellationToken ct)
        {
            lock (_lock)
            {
                var job = GetRequiredJob(jobId);

                if (job is null)
                {
                    return Task.FromResult(JobStateChangeResult.NotFound);
                }

                if (job.LockToken != lockToken)
                {
                    return Task.FromResult(JobStateChangeResult.LockTokenMismatch);
                }

                if (job.Status != JobStatus.Processing)
                {
                    return Task.FromResult(JobStateChangeResult.InvalidState);
                }

                job.Status = JobStatus.Succeeded;
                job.CompletedAt = DateTimeOffset.UtcNow;
                // JobRecord is one execution instance, will not run this exact job record if it succeeds, creates new one
                job.AvailableAt = null;

                // release lock of worker
                job.LockedBy = null;
                job.LockedUntil = null;

                // internal error details
                job.LastErrorMessage = null;
                job.LastErrorType = null;
                job.LastErrorDetails = null;
            }

            return Task.FromResult(JobStateChangeResult.Applied);
        }

        // claim job to specific worker and mark as processing
        public Task<JobRecord?> TryClaimNextRunnableJobAsync(string workerId, TimeSpan lockDuration, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var now = DateTimeOffset.UtcNow;

                var job = _jobs
                    .Where(j =>
                        // only check runnalbe status jobs
                        (j.Status is JobStatus.Enqueued or JobStatus.Scheduled or JobStatus.Retrying) &&
                        (j.AvailableAt is null || j.AvailableAt <= now))
                    .OrderBy(j => j.AvailableAt ?? j.CreatedAt)
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

        public Task<JobStateChangeResult> MarkRetryingAsync(Guid jobId, long lockToken, JobError error, DateTimeOffset availableAt, CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                var job = GetRequiredJob(jobId);

                if (job is null)
                {
                    return Task.FromResult(JobStateChangeResult.NotFound);
                }

                if (job.LockToken != lockToken)
                {
                    return Task.FromResult(JobStateChangeResult.NotFound);
                }

                job.Status = JobStatus.Retrying;

                // internal error details
                job.LastErrorMessage = error.Message;
                job.LastErrorType = error.Type;
                job.LastErrorDetails = error.Details;

                job.AvailableAt = availableAt;

                // if retrying, it has not completed job yet
                job.CompletedAt = null;

                // release lock of worker
                job.LockedBy = null;
                job.LockedUntil = null;
            }

            return Task.FromResult(JobStateChangeResult.Applied);
        }

        public Task<JobStateChangeResult> MarkFailedAsync(Guid jobId, long lockToken, JobError error, CancellationToken ct)
        {
            lock (_lock)
            {
                var job = GetRequiredJob(jobId);

                if (job is null)
                {
                    return Task.FromResult(JobStateChangeResult.NotFound);
                }

                if (job.LockToken != lockToken)
                {
                    return Task.FromResult(JobStateChangeResult.NotFound);
                }

                job.Status = JobStatus.Failed;

                // internal error details
                job.LastErrorMessage = error.Message;
                job.LastErrorType = error.Type;
                job.LastErrorDetails = error.Details;

                job.CompletedAt = DateTimeOffset.UtcNow;

                // releasing locks
                job.LockedBy = null;
                job.LockedUntil = null;

                job.AvailableAt = null;
            }

            return Task.FromResult(JobStateChangeResult.Applied);
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
                AvailableAt = job.AvailableAt,
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
