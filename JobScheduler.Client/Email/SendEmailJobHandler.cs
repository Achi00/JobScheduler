using JobScheduler.Abstractions.Jobs.Contexts;
using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Client.EmailServices;

namespace JobScheduler.Client.Email
{
    // IJobHandler comes from my job scheduler core layer
    public class SendEmailJobHandler : IJobHandler<SendEmailJob>
    {
        private readonly ILogger<SendEmailJobHandler> _logger;

        public SendEmailJobHandler(ILogger<SendEmailJobHandler> logger)
        {
            _logger = logger;
        }
        public async Task HandleAsync(SendEmailJob payload, JobExecutionContext context, CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Executing job {JobId}, attempt {Attempt}. Sending {Template} email to user {UserId}",
                context.JobId,
                context.AttemptCount,
                payload.TemplateName,
                payload.UserId
            );

            await Task.Delay(1000, cancellationToken);
        }
    }
}
