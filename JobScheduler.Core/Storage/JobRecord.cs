using JobScheduler.Abstractions.Jobs.Enums;

namespace JobScheduler.Core.Storage
{
    internal sealed class JobRecord
    {
        public Guid Id { get; set; }
        public string JobType { get; set; } = default!;
        public string PayloadJson { get; set; } = default!;
        public JobStatus Status { get; set; }
        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; } = 3;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? NextRunAt { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public DateTimeOffset? LockedUntil { get; set; }
        public string? LockedBy { get; set; }
        public long LockToken { get; set; }
        public string? LastError { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }
    }
}
