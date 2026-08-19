namespace JobScheduler.Storage.Abstractions.RecurringJobs
{
    public interface IRecurringJobStore
    {
        Task AddOrUpdateAsync(RecurringJobRecord job, CancellationToken cancellationToken);
        Task RemoveAsync(Guid id, CancellationToken cancellationToken);

        // locks + returns due rows, held within the caller's transaction 
        Task<IReadOnlyList<RecurringJobRecord>> GetDueForUpdateAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken);

        Task UpdateNextRunAsync(Guid id, DateTimeOffset nextRunAt, DateTimeOffset lastRunAt, CancellationToken cancellationToken);
    }
}
