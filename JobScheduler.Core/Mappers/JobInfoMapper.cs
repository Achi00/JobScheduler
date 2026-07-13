using JobScheduler.Abstractions.Jobs.Models;
using JobScheduler.Abstractions.Jobs.Structs;
using JobScheduler.Storage.Abstractions.Jobs;

namespace JobScheduler.Core.Mappers
{
    internal class JobInfoMapper
    {
        public static JobInfo ToJobInfo(JobRecord record)
        {
            return new JobInfo(
                Id: JobId.FromGuid(record.Id),
                JobType: record.JobType,
                Status: record.Status,
                AttemptCount: record.AttemptCount,
                MaxAttempts: record.MaxAttempts,
                CreatedAt: record.CreatedAt,
                AvailableAt: record.AvailableAt,
                StartedAt: record.StartedAt,
                CompletedAt: record.CompletedAt,
                LastError: record.LastErrorMessage);
        }
    }
}
