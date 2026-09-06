using JobScheduler.Core.Enums;
using JobScheduler.Storage.Abstractions.Jobs;

namespace JobScheduler.Core.Execution
{
    internal interface IJobProcessor
    {
        Task<JobProcessResult> ProcessAsync(JobRecord job, CancellationToken ct);
    }
}