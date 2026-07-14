using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.EntityFrameworkCore.Persistence.Context;
using JobScheduler.Storage.Abstractions.Jobs;
using JobScheduler.Storage.EntityFrameworkCore.Mappers;
using Microsoft.EntityFrameworkCore;

namespace JobScheduler.EntityFrameworkCore.Storage
{
    public sealed class EntityFrameworkJobStore : IJobStore
    {
        private readonly JobSchedulerDbContext _context;

        public EntityFrameworkJobStore(JobSchedulerDbContext context)
        {
            _context = context;
        }
        public Task CreateAsync(JobRecord job, CancellationToken cancellationToken)
        {
            _context.Add(job);

            return Task.CompletedTask;
        }

        public async Task<JobRecord?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
        {
            var job = await _context.Jobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);

            if (job != null)
            {
                return JobEntityMapper.ToRecord(job);
            }

            return null;
        }

        public async Task<JobStateChangeResult> MarkFailedAsync(Guid jobId, long lockToken, JobError error, CancellationToken cancellationToken)
        {
            var job = await GetJobForUpdate(jobId, cancellationToken);

            if (job is null)
            {
                return JobStateChangeResult.NotFound;
            }

            if (job.LockToken != lockToken)
            {
                return JobStateChangeResult.LockTokenMismatch;
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

            return JobStateChangeResult.Applied;
        }

        public async Task<JobStateChangeResult> MarkRetryingAsync(Guid jobId, long lockToken, JobError error, DateTimeOffset nextRunAt, CancellationToken cancellationToken)
        {
            var job = await GetJobForUpdate(jobId, cancellationToken);

            if (job is null)
            {
                return JobStateChangeResult.NotFound;
            }

            if (job.LockToken != lockToken)
            {
                return JobStateChangeResult.LockTokenMismatch;
            }

            job.Status = JobStatus.Retrying;

            // internal error details
            job.LastErrorMessage = error.Message;
            job.LastErrorType = error.Type;
            job.LastErrorDetails = error.Details;

            job.AvailableAt = nextRunAt;

            // if retrying, it has not completed job yet
            job.CompletedAt = null;

            // release lock of worker
            job.LockedBy = null;
            job.LockedUntil = null;

            return JobStateChangeResult.Applied;
        }

        public Task<JobStateChangeResult> MarkSucceededAsync(Guid jobId, long lockToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<JobRecord?> TryClaimNextRunnableJobAsync(string workerId, TimeSpan lockDuration, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        // includes ef core tracking, can be modified
        private async Task<JobRecord?> GetJobForUpdate(Guid jobId, CancellationToken cancellationToken)
        {
            var job = await _context.Jobs.FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);

            if (job != null)
            {
                return JobEntityMapper.ToRecord(job);
            }

            return null;
        }
    }
}
