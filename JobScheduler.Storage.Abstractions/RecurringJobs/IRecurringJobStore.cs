namespace JobScheduler.Storage.Abstractions.RecurringJobs
{
    internal interface IRecurringJobStore
    {
        Task AddOrUpdateAsync(RecurringJobRecord definition, CancellationToken cancellationToken);
        Task RemoveAsync(string id, CancellationToken cancellationToken);

        // locks + returns due rows, held within the caller's transaction
        Task<IReadOnlyList<RecurringJobRecord>> GetDueForUpdateAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken);

        Task UpdateNextRunAsync(string id, DateTimeOffset nextRunAt, DateTimeOffset lastRunAt, CancellationToken cancellationToken);
    }
}
