using JobScheduler.Abstractions.Jobs.Contexts;
using JobScheduler.Abstractions.Jobs.Interfaces;

namespace JobScheduler.Client.LockTokenTest
{
    public sealed class SlowJobHandler : IJobHandler<SlowJob>
    {
        public async Task HandleAsync(SlowJob payload, JobExecutionContext context, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
        }
    }

    public record SlowJob();
}
