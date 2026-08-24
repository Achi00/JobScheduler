using JobScheduler.Abstractions.Jobs.Interfaces;

namespace JobScheduler.Core.Clients
{
    internal sealed class RecurringJobClient : IRecurringJobClient
    {
        public Task AddOrUpdateAsync<TPayload>(Guid recurringJobId, string cronExpression, TPayload payload, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(Guid recurringJobId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
