using JobScheduler.Abstractions.Jobs.Contexts;

namespace JobScheduler.Core.Execution.Interfaces
{
    internal interface IJobExecutor
    {
        string JobType { get; }
        Task ExecuteAsync(IServiceProvider serviceProvider, string payloadJson, JobExecutionContext context, CancellationToken cancellationToken);
    }
}
