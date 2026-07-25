using JobScheduler.Storage.EntityFrameworkCore.Interfaces;
using System.Data;
using System.Data.Common;
using JobScheduler.Abstractions.Jobs.Enums;

namespace JobScheduler.Storage.SqlServer.Provider
{
    internal sealed class SqlServerJobStoreCommandFactory : IJobStoreCommandFactory
    {
        /*
         * READCOMMITTEDLOCK - uses locks version of data and not snapshot to access this table
         * UPDLOCK - lock candidate for someone who intending to update it
         * READPAST - skip candidates already locked by another worker
         * ROWLOCK - uses only indicidual row or key locking
         * 
         * if RCSI is enabled SQL Server and as default dont looks at current locked row, it reads older commited copy
         * with READPAST it will skip currently locked rows
         * Those two uses different stategies, in this query READCOMMITTEDLOCK is used for it to not read old row version
         * and use locking based read, without this worked coup see older commited version of some job we have in db
         */
        public DbCommand CreateClaimNextRunnableJobCommand(
            DbConnection connection,
            string workerId,
            TimeSpan lockDuration)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentException.ThrowIfNullOrWhiteSpace(workerId);

            if (lockDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(lockDuration),
                    "Lock duration must be greater than zero.");
            }

            var command = connection.CreateCommand();

            command.CommandText = """
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
                        @Enqueued,
                        @Retrying,
                        @Scheduled
                    )
                      AND [AvailableAt] <= @Now
                    ORDER BY
                        [AvailableAt],
                        [CreatedAt]
                )
                UPDATE Candidate
                SET
                    [Status] = @Processing,
                    [LockedBy] = @WorkerId,
                    [LockedUntil] =
                        DATEADD(MILLISECOND, @LockDurationMilliseconds, @Now),
                    [LockToken] = [LockToken] + 1,
                    [AttemptCount] = [AttemptCount] + 1,
                    [AvailableAt] = NULL,
                    [StartedAt] = @Now,
                    [UpdatedAt] = @Now
                OUTPUT
                    INSERTED.[Id],
                    INSERTED.[JobType],
                    INSERTED.[PayloadJson],
                    INSERTED.[Status],
                    INSERTED.[AttemptCount],
                    INSERTED.[MaxAttempts],
                    INSERTED.[CreatedAt],
                    INSERTED.[AvailableAt],
                    INSERTED.[StartedAt],
                    INSERTED.[CompletedAt],
                    INSERTED.[LockedBy],
                    INSERTED.[LockedUntil],
                    INSERTED.[LockToken],
                    INSERTED.[UpdatedAt],
                    INSERTED.[LastErrorMessage],
                    INSERTED.[LastErrorType],
                    INSERTED.[LastErrorDetails];
                """;

            AddParameter(
                command,
                "@WorkerId",
                DbType.String,
                workerId);

            AddParameter(
                command,
                "@LockDurationMilliseconds",
                DbType.Int64,
                checked((long)lockDuration.TotalMilliseconds));

            AddParameter(
                command,
                "@Enqueued",
                DbType.Int32,
                (int)JobStatus.Enqueued);

            AddParameter(
                command,
                "@Retrying",
                DbType.Int32,
                (int)JobStatus.Retrying);

            AddParameter(
                command,
                "@Scheduled",
                DbType.Int32,
                (int)JobStatus.Scheduled);

            AddParameter(
                command,
                "@Processing",
                DbType.Int32,
                (int)JobStatus.Processing);

            return command;
        }

        public DbCommand CreateRecoverExpiredJobsCommand(DbConnection connection, int batchSize, TimeSpan recoveryDelay)
        {
            ArgumentNullException.ThrowIfNull(connection);

            if (batchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(batchSize));
            }

            if (recoveryDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(recoveryDelay));
            }

            var command = connection.CreateCommand();

            command.CommandText = """
            DECLARE @Now datetimeoffset(7) =
                TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00');

            ;WITH ExpiredJobs AS
            (
                SELECT TOP (@BatchSize) *
                FROM [dbo].[Jobs] WITH
                (
                    UPDLOCK,
                    READPAST,
                    ROWLOCK,
                    READCOMMITTEDLOCK
                )
                WHERE [Status] = @Processing
                  AND [LockedUntil] IS NOT NULL
                  AND [LockedUntil] <= @Now
                ORDER BY
                    [LockedUntil],
                    [Id]
            )
            UPDATE ExpiredJobs
            SET
                [Status] =
                    CASE
                        WHEN [AttemptCount] >= [MaxAttempts]
                            THEN @Failed
                        ELSE @Retrying
                    END,

                [AvailableAt] =
                    CASE
                        WHEN [AttemptCount] >= [MaxAttempts]
                            THEN NULL
                        ELSE DATEADD(
                            MILLISECOND,
                            @RecoveryDelayMilliseconds,
                            @Now)
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
                [UpdatedAt] = @Now;
            """;

            AddParameter(
                command,
                "@BatchSize",
                DbType.Int32,
                batchSize);

            AddParameter(
                command,
                "@RecoveryDelayMilliseconds",
                DbType.Int64,
                checked((long)recoveryDelay.TotalMilliseconds));

            AddParameter(
                command,
                "@Processing",
                DbType.Int32,
                (int)JobStatus.Processing);

            AddParameter(
                command,
                "@Retrying",
                DbType.Int32,
                (int)JobStatus.Retrying);

            AddParameter(
                command,
                "@Failed",
                DbType.Int32,
                (int)JobStatus.Failed);

            return command;
        }

        private static void AddParameter(
            DbCommand command,
            string name,
            DbType type,
            object? value)
        {
            var parameter = command.CreateParameter();

            parameter.ParameterName = name;
            parameter.DbType = type;
            parameter.Value = value ?? DBNull.Value;

            command.Parameters.Add(parameter);
        }
    }
}
