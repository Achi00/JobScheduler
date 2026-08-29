using JobScheduler.Abstractions.Jobs.Contexts;
using JobScheduler.Abstractions.Jobs.Interfaces;

namespace JobScheduler.Client.LockTokenTest
{
    public sealed class SlowJobHandler : IJobHandler<SlowJob>
    {
        public Task HandleAsync(SlowJob payload, JobExecutionContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    public record SlowJob();
}
