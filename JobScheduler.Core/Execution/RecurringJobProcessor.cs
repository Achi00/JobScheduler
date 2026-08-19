using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.Core.Options;
using JobScheduler.Core.Recurring;
using JobScheduler.Storage.Abstractions.Jobs;
using JobScheduler.Storage.Abstractions.RecurringJobs;
using JobScheduler.Storage.Abstractions.UnitOfWork;
using Microsoft.Extensions.Options;

namespace JobScheduler.Core.Execution
{
    internal sealed class RecurringJobProcessor
    {
        private readonly IRecurringJobStore _recurringStore;
        private readonly IJobStore _jobStore;
        private readonly ICronScheduler _cronScheduler;
        private readonly TimeProvider _timeProvider;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOptionsMonitor<JobSchedulerOptions> _options;

        public RecurringJobProcessor(
            IRecurringJobStore recurringStore, 
            IJobStore jobStore, 
            ICronScheduler cronScheduler, 
            TimeProvider timeProvider,
            IUnitOfWork unitOfWork,
            IOptionsMonitor<JobSchedulerOptions> options)
        {
            _recurringStore = recurringStore;
            _jobStore = jobStore;
            _cronScheduler = cronScheduler;
            _timeProvider = timeProvider;
            _unitOfWork = unitOfWork;
            _options = options;
        }

        public async Task DispatchDueJobsAsync(CancellationToken ct)
        {
            var now = _timeProvider.GetUtcNow();

            await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);

            var due = await _recurringStore.GetDueForUpdateAsync(now, batchSize: 50, ct);

            foreach (var definition in due)
            {
                var nextRunAt = _cronScheduler.GetNextOccurrence(definition.CronExpression, definition.TimeZoneId, now);

                await _jobStore.CreateAsync(new JobRecord
                {
                    Id = Guid.NewGuid(),
                    JobType = definition.JobType,
                    PayloadJson = definition.PayloadJson,
                    Status = JobStatus.Enqueued,
                    CreatedAt = now,
                    AvailableAt = now,
                    MaxAttempts = _options.CurrentValue.DefaultMaxAttempts
                }, ct);

                await _recurringStore.UpdateNextRunAsync(definition.Id, nextRunAt, now, ct);
            }

            await transaction.CommitAsync(ct);
        }
    }
}
