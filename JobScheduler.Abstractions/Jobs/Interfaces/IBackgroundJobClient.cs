namespace JobScheduler.Abstractions.Jobs.Interfaces
{
    public interface IBackgroundJobClient
    {
        Task<Guid> EnqueueAsync<TPayload>(TPayload payload, CancellationToken cancellationToken = default);
        Task<Guid> ScheduleAsync<TPayload>(TPayload payload, DateTimeOffset runAt, CancellationToken cancellationToken = default);
    }
}
