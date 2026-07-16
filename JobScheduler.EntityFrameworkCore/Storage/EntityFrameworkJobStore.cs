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
            ArgumentException.ThrowIfNullOrWhiteSpace(workerId);

            if (lockDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lockDuration),
                    "Lock duration must be greater than zero.");
            }

            var lockDurationMilliseconds = checked((int)lockDuration.TotalMilliseconds);

            var claimed = await _context.Jobs.FromSqlInterpolated($"""
            DECLARE @Now datetimeoffset(7) =
                TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');

            ;WITH Candidate AS
            (
                SELECT TOP (1) *
                FROM [dbo].[Jobs] WITH
                (
                    UPDLOCK,
                    READPAST,
                    ROWLOCK,
                    READCOMMITTEDLOCK
                )
                WHERE [Status] IN
                (
                    {(int)JobStatus.Enqueued},
                    {(int)JobStatus.Retrying},
                    {(int)JobStatus.Scheduled}
                )
                  AND [AvailableAt] <= @Now
                  AND
                  (
                      [LockedUntil] IS NULL
                      OR [LockedUntil] <= @Now
                  )
                ORDER BY
                    [AvailableAt],
                    [CreatedAt]
            )
            UPDATE Candidate
            SET
                [Status] = {(int)JobStatus.Processing},
                [LockedBy] = {workerId},
                [LockedUntil] =
                    DATEADD(MILLISECOND, {lockDurationMilliseconds}, @Now),
                [LockToken] = [LockToken] + 1,
                [AttemptCount] = [AttemptCount] + 1,
                [StartedAt] = @Now,
                [UpdatedAt] = @Now
            OUTPUT INSERTED.*;
            """).AsNoTracking().ToListAsync(cancellationToken);

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
