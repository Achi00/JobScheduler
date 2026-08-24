namespace JobScheduler.Abstractions.Jobs.Interfaces
{
    public interface IRecurringJobClient
    {
        Task AddOrUpdateAsync<TPayload>(
            Guid recurringJobId,
            string cronExpression,
            TPayload payload,
            CancellationToken cancellationToken = default);

        Task RemoveAsync(
            Guid recurringJobId,
            CancellationToken cancellationToken = default);
    }
}
