using JobScheduler.EntityFrameworkCore.Interfaces;
using JobScheduler.EntityFrameworkCore.Mappers;
using JobScheduler.EntityFrameworkCore.Persistence.Context;
using JobScheduler.EntityFrameworkCore.Readers;
using JobScheduler.Storage.Abstractions.RecurringJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace JobScheduler.EntityFrameworkCore.Storage
{
    // follows same idea as IJobStore, server locking
    public sealed class EntityFrameworkRecurringJobStore : IRecurringJobStore
    {
        private readonly JobSchedulerDbContext _context;
        private readonly IRecurringJobStoreCommandFactory _providerFactory;

        public EntityFrameworkRecurringJobStore(JobSchedulerDbContext context, IRecurringJobStoreCommandFactory providerFactory)
        {
            _context = context;
            _providerFactory = providerFactory;
        }
        public async Task AddOrUpdateAsync(RecurringJobRecord job, CancellationToken cancellationToken)
        {
            var existing = await _context.RecurringJob.FirstOrDefaultAsync(x => x.Id == job.Id, cancellationToken);

            if (existing is null)
            {
                _context.RecurringJob.Add(RecurringJobEntityMapper.ToEntity(job));
            }
            else
            {
                RecurringJobEntityMapper.ApplyTo(existing, job);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<RecurringJobRecord>> GetDueForUpdateAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken)
        {
            // raw sql connection
            var connection = _context.Database.GetDbConnection();
            var shouldClose = connection.State != ConnectionState.Open;
            if (shouldClose)
            {
                await connection.OpenAsync(cancellationToken);
            }

            try
            {
                await using var command = _providerFactory.CreateGetDueForUpdateCommand(connection, now, batchSize);

                var currentTransaction = _context.Database.CurrentTransaction;

                if (currentTransaction != null)
                {
                    command.Transaction = currentTransaction.GetDbTransaction();
                }

                var results = new List<RecurringJobRecord>();
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var entity = RecurringJobEntityDataReader.Read(reader);
                    results.Add(RecurringJobEntityMapper.ToRecord(entity));
                }

                return results;
            }
            finally
            {
                if (shouldClose)
                {
                    await connection.CloseAsync();
                }
            }
        }

        public async Task RemoveAsync(Guid id, CancellationToken cancellationToken)
        {
            await _context.RecurringJob.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);
        }

        public async Task UpdateNextRunAsync(Guid id, DateTimeOffset nextRunAt, DateTimeOffset lastRunAt, CancellationToken cancellationToken)
        {
            await _context.RecurringJob
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(
                    setter => setter
                        .SetProperty(x => x.NextRunAt, nextRunAt)
                        .SetProperty(x => x.LastRunAt, lastRunAt)
                , cancellationToken);
        }
    }
}
