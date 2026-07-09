using JobScheduler.Abstractions.Jobs.Models;
using JobScheduler.Abstractions.Jobs.Structs;

namespace JobScheduler.Abstractions.Jobs.Interfaces
{
    // reads job state
    public interface IBackgroundJobReader
    {
        Task<JobInfo?> GetByIdAsync(JobId jobId, CancellationToken cancellationToken = default);
    }
}
