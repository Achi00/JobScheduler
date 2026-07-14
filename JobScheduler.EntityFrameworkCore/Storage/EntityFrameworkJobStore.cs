using JobScheduler.EntityFrameworkCore.Persistence.Context;
using JobScheduler.Storage.Abstractions.Jobs;

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
            throw new NotImplementedException();
        }

        public Task<JobRecord?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<JobStateChangeResult> MarkFailedAsync(Guid jobId, long lockToken, JobError error, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<JobStateChangeResult> MarkRetryingAsync(Guid jobId, long lockToken, JobError error, DateTimeOffset nextRunAt, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<JobStateChangeResult> MarkSucceededAsync(Guid jobId, long lockToken, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<JobRecord?> TryClaimNextRunnableJobAsync(string workerId, TimeSpan lockDuration, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
