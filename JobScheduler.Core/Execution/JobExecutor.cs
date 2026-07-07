using JobScheduler.Abstractions.Jobs.Contexts;
using JobScheduler.Abstractions.Jobs.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace JobScheduler.Core.Execution
{
    // stored json job -> strongly typed user handler
    internal sealed class JobExecutor<TPayload, THandler> : IJobExecutor where THandler : IJobHandler<TPayload>
    {
        public string JobType { get; }
        public JobExecutor(string jobType)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jobType);

            JobType = jobType;
        }

        // deserialize json, resolve handler from DI, call HandleAsync
        public async Task ExecuteAsync(IServiceProvider serviceProvider, string payloadJson, JobExecutionContext context, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<TPayload>(payloadJson);

            if (payload is null)
            {
                throw new InvalidOperationException($"Failed to deserialize payload for job type {JobType}");
            }

            var handler = serviceProvider.GetRequiredService<THandler>();

            await handler.HandleAsync(payload, context, cancellationToken);
        }
    }
}
