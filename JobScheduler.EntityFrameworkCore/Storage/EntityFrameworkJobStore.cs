using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.EntityFrameworkCore.Entities;
using JobScheduler.EntityFrameworkCore.Persistence.Context;
using JobScheduler.Storage.Abstractions.Jobs;
using JobScheduler.Storage.EntityFrameworkCore.Interfaces;
using JobScheduler.Storage.EntityFrameworkCore.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace JobScheduler.EntityFrameworkCore.Storage
{
    // TODO: add strict expiration together with a heartbeat/lease-renewal mechanism
    // currently only LockToken matching is used

    /*
     * TODO: TryClaimNextRunnableJobAsync, RecoverExpiredJobsAsync, ValidateReadCommittedSnapshotAsync 
     * should be moved in SQL Server specific project 
     */
    public sealed class EntityFrameworkJobStore : IJobStore
    {
        private readonly JobSchedulerDbContext _context;
        private readonly IJobStoreProviderOperations _providerOperations;

        public EntityFrameworkJobStore(JobSchedulerDbContext context, IJobStoreProviderOperations providerOperations)
        {
            _context = context;
            _providerOperations = providerOperations;
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
            var now = DateTimeOffset.UtcNow;

            var affectedRows = await _context.Jobs
                .Where(job => job.Id == jobId && job.Status == JobStatus.Processing && job.LockToken == lockToken)
                .ExecuteUpdateAsync(
                    setter => setter
                        .SetProperty(
                            job => job.Status,
                            JobStatus.Failed)
                        .SetProperty(
                            job => job.CompletedAt,
                            now)
                        .SetProperty(
                            job => job.UpdatedAt,
                            now)
                        .SetProperty(
                            job => job.LockedBy,
                            (string?)null)
                        .SetProperty(
                            job => job.LockedUntil,
                            (DateTimeOffset?)null)
                        .SetProperty(
                            job => job.AvailableAt,
                            (DateTimeOffset?)null)
                        .SetProperty(
                            job => job.LastErrorDetails,
                            error.Details)
                        .SetProperty(
                            job => job.LastErrorMessage,
                            error.Message)
                        .SetProperty(
                            job => job.LastErrorType,
                            error.Type),
                cancellationToken);

            if (affectedRows == 1)
            {
                return JobStateChangeResult.Applied;
            }

            return await DetermineStateChangeFailureAsync(
                jobId,
                lockToken,
                cancellationToken);
        }

        public async Task<JobStateChangeResult> MarkRetryingAsync(Guid jobId, long lockToken, JobError error, DateTimeOffset nextRunAt, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            var affectedRows = await _context.Jobs
                .Where(job => job.Id == jobId && job.Status == JobStatus.Processing && job.LockToken == lockToken)
                .ExecuteUpdateAsync(
                    setter => setter
                        .SetProperty(
                            job => job.Status,
                            JobStatus.Retrying)
                        .SetProperty(
                            job => job.CompletedAt,
                            (DateTimeOffset?)null)
                        .SetProperty(
                            job => job.UpdatedAt,
                            now)
                        .SetProperty(
                            job => job.LockedBy,
                            (string?)null)
                        .SetProperty(
                            job => job.LockedUntil,
                            (DateTimeOffset?)null)
                        .SetProperty(
                            job => job.AvailableAt,
                            nextRunAt)
                        .SetProperty(
                            job => job.LastErrorDetails,
                            error.Details)
                        .SetProperty(
                            job => job.LastErrorMessage,
                            error.Message)
                        .SetProperty(
                            job => job.LastErrorType,
                            error.Type),
                cancellationToken);

            if (affectedRows == 1)
            {
                return JobStateChangeResult.Applied;
            }

            return await DetermineStateChangeFailureAsync(
                jobId,
                lockToken,
                cancellationToken);
        }

        public async Task<JobStateChangeResult> MarkSucceededAsync(Guid jobId, long lockToken, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;

            // ExecuteUpdateAsync needs no saveCahnges does not affects ef tracking
            var affectedRows = await _context.Jobs
                .Where(job => job.Id == jobId && job.Status == JobStatus.Processing && job.LockToken == lockToken)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(
                            job => job.Status,
                            JobStatus.Succeeded)
                        .SetProperty(
                            job => job.CompletedAt,
                            now)
                        .SetProperty(
                            job => job.UpdatedAt,
                            now)
                        .SetProperty(
                            job => job.LockedBy,
                            (string?)null)
                        .SetProperty(
                            job => job.LockedUntil,
                            (DateTimeOffset?)null)
                        .SetProperty(
                            job => job.AvailableAt,
                            (DateTimeOffset?)null),
                cancellationToken);

            if (affectedRows == 1)
            {
                return JobStateChangeResult.Applied;
            }

            return await DetermineStateChangeFailureAsync(
                jobId,
                lockToken,
                cancellationToken);
        }
        // handles jobs where status processing + expired jobs to retrying or failed
        // incrementing token while recovering, bacause old worker with old LockToken should not be able to carry on expired job after it hang or slept
        public async Task<int> RecoverExpiredJobsAsync(int batchSize, TimeSpan recoveryDelay, CancellationToken cancellationToken)
        {
            if (batchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(batchSize),
                    "Batch size must be greater than zero.");
            }

            if (recoveryDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(recoveryDelay),
                    "Recovery delay can not be negative.");
            }

            var recoveryDelayMilliseconds = checked((int)recoveryDelay.TotalMilliseconds);

            var affectedRows = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @Now datetimeoffset(7) =
                    TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');

                ;WITH ExpiredJobs AS
                (
                    SELECT TOP ({batchSize}) *
                    FROM [dbo].[Jobs] WITH
                    (
                        UPDLOCK,
                        READPAST,
                        ROWLOCK,
                        READCOMMITTEDLOCK
                    )
                    WHERE [Status] = @Processing
                      AND [LockedUntil] IS NOT NULL
                      AND [LockedUntil]<= @Now
                    ORDER BY [LockedUntil], [Id]
                )
                UPDATE ExpiredJobs
                SET
                    [Status] =
                        CASE
                            WHEN [AttemptCount] >= [MaxAttempts]
                                THEN {(int)JobStatus.Failed}
                            ELSE {(int)JobStatus.Retrying}
                        END,

                    [AvailableAt] =
                        CASE
                            WHEN [AttemptCount] >= [MaxAttempts]
                                THEN NULL
                            ELSE DATEADD(MILLISECOND, {recoveryDelayMilliseconds}, @Now)
                        END,

                    [CompletedAt] =
                        CASE
                            WHEN [AttemptCount] >= [MaxAttempts]
                                THEN @Now
                            ELSE NULL
                        END,

                    [LastErrorMessage] =
                    'Worker lease expired before completion.',

                    [LastErrorType] =
                        'JobLeaseExpired',

                    [LastErrorDetails] = NULL,

                    [LockedBy] = NULL,
                    [LockedUntil] = NULL,

                    [LockToken] = [LockToken] + 1,

                    UpdatedAt = @Now;
            """, cancellationToken);

            return affectedRows;
        }

        // TESTING: trying to use READPAST/UPDLOCK/ROWLOCK so no worker can access and lock job between select and update
        public async Task<JobRecord?> TryClaimNextRunnableJobAsync(string workerId, TimeSpan lockDuration, CancellationToken cancellationToken)
        {
            var connection = _context.Database.GetDbConnection();

            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var command = _providerOperations.CreateClaimNextRunnableJobCommand(connection, workerId, lockDuration);

                var currentTransaction = _context.Database.CurrentTransaction;

                if (currentTransaction != null)
                {
                    command.Transaction = currentTransaction.GetDbTransaction();
                }

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            }
            catch (Exception)
            {
                

            }
                return null;
        }

        // returns appropriate job state value base on what condition job is at
        private async Task<JobStateChangeResult> DetermineStateChangeFailureAsync(
            Guid jobId,
            long expectedLockToken,
            CancellationToken cancellationToken)
        {
            var job = await _context.Jobs
                .AsNoTracking()
                .Where(job => job.Id == jobId)
                .Select(job => new
                {
                    job.Status,
                    job.LockToken
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (job is null)
            {
                return JobStateChangeResult.NotFound;
            }

            if (job.LockToken != expectedLockToken)
            {
                return JobStateChangeResult.LockTokenMismatch;
            }

            return JobStateChangeResult.InvalidState;
        }
    }
}
