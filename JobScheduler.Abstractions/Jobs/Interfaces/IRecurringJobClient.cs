namespace JobScheduler.Abstractions.Jobs.Interfaces
{
    public interface IRecurringJobClient
    {
        Task AddOrUpdateAsync<TPayload>(
            string recurringJobId,
            string cronExpression,
            TPayload payload,
            CancellationToken cancellationToken = default);

        Task RemoveAsync(
            string recurringJobId,
            CancellationToken cancellationToken = default);
    }
}
