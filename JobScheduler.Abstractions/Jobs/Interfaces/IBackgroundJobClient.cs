using JobScheduler.Abstractions.Jobs.Structs;

namespace JobScheduler.Abstractions.Jobs.Interfaces
{
    // creates jobs
    public interface IBackgroundJobClient
    {
        Task<JobId> EnqueueAsync<TPayload>(TPayload payload, CancellationToken cancellationToken = default);
        Task<JobId> ScheduleAsync<TPayload>(TPayload payload, DateTimeOffset runAt, CancellationToken cancellationToken = default);
    }
}
