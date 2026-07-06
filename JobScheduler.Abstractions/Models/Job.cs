using JobScheduler.Abstractions.Enums;

namespace JobScheduler.Abstractions.Models
{
    public class Job
    {
        public Guid Id { get; set; }
        public string JobType { get; set; } = default!;
        public string PayloadJson { get; set; } = default!;
        public JobStatus Status { get; set; }
        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; } = 3;
        public DateTime CreatedAt { get; set; }

        // null = run ASAP; future = scheduled/retry
        public DateTime? NextRunAt { get; set; }
        public DateTime? LockedAt { get; set; }
        // worker instance id, for diagnosing stuck jobs
        public string? LockedBy { get; set; }
        public string? LastError { get; set; }
        // null for one-off jobs, set for recurring
        public string? CronExpression { get; set; }
        // soft delete, job 30 Days old
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
