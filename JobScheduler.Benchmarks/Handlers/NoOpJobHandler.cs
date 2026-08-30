using JobScheduler.Abstractions.Jobs.Contexts;
using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Benchmarks.Models;

namespace JobScheduler.Benchmarks.Handlers
{
    // handler registered in console app, no handler work to do!!!
    internal sealed class NoOpJobHandler : IJobHandler<NoOpJob>
    {
        public Task HandleAsync(NoOpJob payload, JobExecutionContext context, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
