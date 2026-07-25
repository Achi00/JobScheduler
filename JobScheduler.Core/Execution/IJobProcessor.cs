using JobScheduler.Core.Enums;

namespace JobScheduler.Core.Execution
{
    internal interface IJobProcessor
    {
        Task<JobProcessResult> TryProcessOneAsync(string workerId, CancellationToken ct);
    }
}