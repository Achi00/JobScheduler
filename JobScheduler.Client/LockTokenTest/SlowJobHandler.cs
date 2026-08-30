using JobScheduler.Abstractions.Jobs.Contexts;
using JobScheduler.Abstractions.Jobs.Interfaces;

namespace JobScheduler.Client.LockTokenTest
{
    public sealed class SlowJobHandler : IJobHandler<SlowJob>
    {
        private readonly ILogger<SlowJobHandler> _logger;

        public SlowJobHandler(ILogger<SlowJobHandler> logger)
        {
            _logger = logger;
        }
        public async Task HandleAsync(SlowJob payload, JobExecutionContext context, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Executing slow job {JobId}, attempt {Attempt}",
                context.JobId,
                context.AttemptCount
            );

            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }
    }

    public record SlowJob(string name);
}
