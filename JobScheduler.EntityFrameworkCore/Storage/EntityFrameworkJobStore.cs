using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.EntityFrameworkCore.Entities;
using JobScheduler.EntityFrameworkCore.Persistence.Context;
using JobScheduler.Storage.Abstractions.Jobs;
using JobScheduler.Storage.EntityFrameworkCore.Interfaces;
using JobScheduler.Storage.EntityFrameworkCore.Mappers;
using JobScheduler.Storage.EntityFrameworkCore.Readers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

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
            //SqlServerJobStoreCommandFactory : IJobStoreCommandFactory
        private readonly IJobStoreCommandFactory _providerFactory;

        public EntityFrameworkJobStore(JobSchedulerDbContext context, IJobStoreCommandFactory providerOperations)
        {
            _context = context;
            _providerFactory = providerOperations;
        }
        public async Task CreateAsync(JobRecord job, CancellationToken cancellationToken)
        {
            var entity = JobEntityMapper.ToEntity(job);
            _context.Jobs.Add(entity);

            await _context.SaveChangesAsync(cancellationToken);
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
        // no async, avoid spaming async state machine
        public Task<int> RecoverExpiredJobsAsync(int batchSize, TimeSpan recoveryDelay, CancellationToken cancellationToken)
        {
            // gets or opens connection, returnes job recovery command db query
            return ExecuteProviderCommandAsync(
                connection =>
                    _providerFactory.CreateRecoverExpiredJobsCommand(
                        connection,
                        batchSize,
                        recoveryDelay),
                        // static lampda wont capture outside local variables or this
                        // could be benefitial to avoid closure allocation??
                        static (command, ct) => command.ExecuteNonQueryAsync(ct),
                cancellationToken);
        }

        // TESTING: trying to use READPAST/UPDLOCK/ROWLOCK so no worker can access and lock job between select and update
        // no async, avoids spaming async state machine
        public Task<JobRecord?> TryClaimNextRunnableJobAsync(string workerId, TimeSpan lockDuration, CancellationToken cancellationToken)
        {
            return ExecuteProviderCommandAsync(
                connection =>
                    _providerFactory.CreateClaimNextRunnableJobCommand(
                        connection,
                        workerId,
                        lockDuration),
                ReadClaimedJobAsync, 
                cancellationToken);
        }

        // gets/opens connection, creates command, attach to current transaction, execute, close if this method opened it
        private async Task<TResult> ExecuteProviderCommandAsync<TResult>(
            Func<DbConnection, DbCommand> commandFactory,
            Func<DbCommand, CancellationToken, Task<TResult>> execute,
            CancellationToken cancellationToken)
        {
            var connection = _context.Database.GetDbConnection();

            var shouldClose = connection.State != ConnectionState.Open;

            if (shouldClose)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var command = commandFactory(connection);

                var currentTransaction = _context.Database.CurrentTransaction;

                if (currentTransaction != null)
                {
                    command.Transaction = currentTransaction.GetDbTransaction();
                }

                return await execute(command, cancellationToken);
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
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

        // db -> JobRecord
        private static async Task<JobRecord?> ReadClaimedJobAsync(
            DbCommand command,
            CancellationToken cancellationToken)
        {
            await using var reader =
                await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var entity = JobEntityDataReader.Read(reader);

            if (await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException(
                    "The claim operation returned more than one job.");
            }

            return JobEntityMapper.ToRecord(entity);
        }
    }
}
