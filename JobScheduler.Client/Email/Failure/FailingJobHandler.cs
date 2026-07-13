using JobScheduler.Abstractions.Jobs.Contexts;
using JobScheduler.Abstractions.Jobs.Interfaces;

namespace JobScheduler.Client.Email.Failure
{
    // simulates failure
    public class FailingJobHandler : IJobHandler<FailingJob>
    {
        private readonly ILogger<FailingJobHandler> _logger;

        public FailingJobHandler(ILogger<FailingJobHandler> logger)
        {
            _logger = logger;
        }
        public Task HandleAsync(FailingJob payload, JobExecutionContext context, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Executing failing job {JobId}, attempt {Attempt}",
                context.JobId,
                context.AttemptCount
            );

            throw new InvalidOperationException("Simulated testing failure");
        }
    }
}
