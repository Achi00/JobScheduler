using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Core.Options;
using JobScheduler.Core.Recurring.Interfaces;
using JobScheduler.Core.Resolvers;
using JobScheduler.Storage.Abstractions.RecurringJobs;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace JobScheduler.Core.Clients
{
    internal sealed class RecurringJobClient : IRecurringJobClient
    {
        private readonly IRecurringJobStore _store;
        private readonly ICronScheduler _cronScheduler;
        private readonly TimeProvider _timeProvider;

        public RecurringJobClient(IRecurringJobStore store, ICronScheduler cronScheduler, TimeProvider timeProvider)
        {
            _store = store;
            _cronScheduler = cronScheduler;
            _timeProvider = timeProvider;
        }
        public async Task AddOrUpdateAsync<TPayload>(Guid recurringJobId, string cronExpression, TPayload payload, string timeZoneId = "UTC", CancellationToken cancellationToken = default)
        {
            var now = _timeProvider.GetUtcNow();

            // validates string cron expression, throws if invalid
            var nextRunAt = _cronScheduler.GetNextOccurrence(cronExpression, timeZoneId, now);

            var record = new RecurringJobRecord(
                Id: recurringJobId,
                // attribute based type resolver, if no attribute provided fallspack on typeof(T).FillName
                JobType: JobTypeNameResolver.Resolve<TPayload>(),
                PayloadJson: JsonSerializer.Serialize(payload),
                CronExpression: cronExpression,
                TimeZoneId: TimeZoneInfo.Utc.Id,
                IsEnabled: true,
                NextRunAt: nextRunAt,
                LastRunAt: null
            );

            await _store.AddOrUpdateAsync(record, cancellationToken);
        }

        public async Task RemoveAsync(Guid recurringJobId, CancellationToken cancellationToken = default)
        {
            await _store.RemoveAsync(recurringJobId, cancellationToken);
        }
    }
}
