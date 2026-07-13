using JobScheduler.Abstractions.Jobs.Interfaces;
using JobScheduler.Abstractions.Jobs.Models;
using JobScheduler.Abstractions.Jobs.Structs;
using JobScheduler.Core.Mappers;
using JobScheduler.Storage.Abstractions.Jobs;

namespace JobScheduler.Core
{
    internal class BackgroundJobReader : IBackgroundJobReader
    {
        private readonly IJobStore _jobs;
        public BackgroundJobReader(IJobStore jobs)
        {
            _jobs = jobs;
        }

        public async Task<JobInfo?> GetByIdAsync(JobId jobId, CancellationToken cancellationToken = default)
        {
            var record = await _jobs.GetByIdAsync(jobId.Value, cancellationToken);

            if (record == null)
            {
                return null;
            }

            // do manual mapping from JobRecord to JobInfo
            return JobInfoMapper.ToJobInfo(record);
        }
    }
}
