namespace JobScheduler.Storage.Abstractions.RecurringJobs
{
    /*
     * follows same principle as IJobstore, in case of ef core also should implament server locking READPAST/UPDLOCK,
     * so it can behave as expected in case of multiple app instances and will scale horizontally
     * otherwise they will race for same RecurringJob rows
    */
    public interface IRecurringJobStore
    {
        Task AddOrUpdateAsync(RecurringJobRecord job, CancellationToken cancellationToken);
        Task RemoveAsync(Guid id, CancellationToken cancellationToken);

        // locks + returns due rows, held within the caller's transaction 
        Task<IReadOnlyList<RecurringJobRecord>> GetDueForUpdateAsync(DateTimeOffset now, int batchSize, CancellationToken cancellationToken);

        Task UpdateNextRunAsync(Guid id, DateTimeOffset nextRunAt, DateTimeOffset lastRunAt, CancellationToken cancellationToken);
    }
}
