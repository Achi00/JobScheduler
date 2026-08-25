using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Core.Options;
using JobScheduler.Core.Resolvers;
using JobScheduler.Storage.Abstractions.RecurringJobs;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace JobScheduler.Core.Clients
{
    internal sealed class RecurringJobClient : IRecurringJobClient
    {
        private readonly IRecurringJobStore _store;

        public RecurringJobClient(IRecurringJobStore store)
        {
            _store = store;
        }
        public async Task AddOrUpdateAsync<TPayload>(Guid recurringJobId, string cronExpression, TPayload payload, CancellationToken cancellationToken = default)
        {
            var record = new RecurringJobRecord(
                Id: recurringJobId,
                JobType: JobTypeNameResolver.Resolve<TPayload>(),
                PayloadJson: JsonSerializer.Serialize(payload),
                CronExpression: cronExpression,
                TimeZoneId: TimeZoneInfo.Utc.Id,
                IsEnabled: true,
                NextRunAt: null,
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
