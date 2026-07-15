using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.EntityFrameworkCore.Entities;
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

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return JobStateChangeResult.Applied;
            }
            catch (DbUpdateConcurrencyException)
            {
                // token was changed between load and save
                return JobStateChangeResult.LockTokenMismatch;
            }
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

        public async Task<JobStateChangeResult> MarkSucceededAsync(Guid jobId, long lockToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        // TESTING: trying to use READPAST/UPDLOCK/ROWLOCK so no worker can access and lock job between select and update
        public async Task<JobRecord?> TryClaimNextRunnableJobAsync(string workerId, TimeSpan lockDuration, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            var lockedUntil = now.Add(lockDuration);
            var newLockToken = DateTime.UtcNow.Ticks;

            var claimed = await _context.Jobs.FromSqlInterpolated($@"
                WITH Candidate AS (
                    SELECT TOP (1) *
                    FROM Jobs WITH (READPAST, UPDLOCK, ROWLOCK, READCOMMITTEDLOCK)
                    WHERE Status IN ({(int)JobStatus.Enqueued}, {(int)JobStatus.Retrying}, {(int)JobStatus.Scheduled})
                      AND AvailableAt <= {now}
                      AND 
                      (
                            LockedUntil IS NULL OR LockedUntil <= SYSUTCDATETIME()
                      )
                    ORDER BY AvailableAt, CreatedAt
                )
                UPDATE Candidate
                SET Status = {(int)JobStatus.Processing},
                    LockedBy = {workerId},
                    LockedUntil = {lockedUntil},
                    LockToken = {newLockToken}
                    AttemptCount = AttemptCount + 1,
                    StartedAt = SYSUTCDATETIME(),
                    UpdatedAt = SYSUTCDATETIME()
                OUTPUT INSERTED.*;
            ").AsNoTracking().ToListAsync(cancellationToken);

            var entity = claimed.SingleOrDefault();
            return entity is null ? null : JobEntityMapper.ToRecord(entity);
        }

        // includes ef core tracking, can be modified
        private async Task<JobEntity?> GetJobForUpdate(Guid jobId, CancellationToken cancellationToken)
        {
            var job = await _context.Jobs.FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);

            return job;
        }
    }
}
