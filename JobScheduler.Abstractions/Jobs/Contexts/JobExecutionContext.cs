using JobScheduler.Abstractions.Jobs.Structs;

namespace JobScheduler.Abstractions.Jobs.Contexts
{
    // readonly, encapsulated internal data of jobs without exposing JobRecord
    public sealed class JobExecutionContext
    {
        public JobId JobId{ get; }
        public string JobType { get; }
        public int AttemptCount { get; }
        public DateTimeOffset CreatedAt { get; }
        public DateTimeOffset StartedAt { get; }

        public JobExecutionContext(JobId jobId, string jobType, int attemptCount, DateTimeOffset createdAt, DateTimeOffset startedAt)
        {
            JobId = jobId;
            JobType = jobType;
            AttemptCount = attemptCount;
            CreatedAt = createdAt;
            StartedAt = startedAt;
        }
    }
}
