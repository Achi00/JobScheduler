using JobScheduler.Abstractions.Jobs.Contexts;

namespace JobScheduler.Abstractions.Jobs.Interfaces
{
    // user implamented
    public interface IJobHandler<in TPayload>
    {
        Task HandleAsync(TPayload payload, JobExecutionContext context, CancellationToken cancellationToken);
    }
}
