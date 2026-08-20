using JobScheduler.EntityFrameworkCore.Persistence.Context;
using JobScheduler.Storage.Abstractions.RecurringJobs;

namespace JobScheduler.EntityFrameworkCore.Storage
{
    // follows same idea as IJobStore, server locking
    public sealed class EntityFrameworkRecurringJobStore : IRecurringJobStore
    {
        private readonly JobSchedulerDbContext _context;

        public EntityFrameworkRecurringJobStore(JobSchedulerDbContext context)
        {
            _context = context;
        }
        public Task AddOrUpdateAsync(RecurringJobRecord job, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<RecurringJobRecord>> GetDueForUpdateAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(Guid id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UpdateNextRunAsync(Guid id, DateTimeOffset nextRunAt, DateTimeOffset lastRunAt, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
