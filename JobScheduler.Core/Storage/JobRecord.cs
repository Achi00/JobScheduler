using JobScheduler.Abstractions.Jobs.Enums;

namespace JobScheduler.Core.Storage
{
    // when one execution should run/retry
    // when the next occurrence should be created we use RecurringJobRecord
    internal sealed class JobRecord
    {
        public Guid Id { get; set; }
        public string JobType { get; set; } = default!;
        public string PayloadJson { get; set; } = default!;
        public JobStatus Status { get; set; }
        public int AttemptCount { get; set; }
        public int MaxAttempts { get; set; } = 3;
        public DateTimeOffset CreatedAt { get; set; }
        // used not as scheduled job next execution but for first run or on retry
        public DateTimeOffset? AvailableAt { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public DateTimeOffset? LockedUntil { get; set; }
        public string? LockedBy { get; set; }
        public long LockToken { get; set; }
        public bool IsDeleted { get; set; }
        public DateTimeOffset? DeletedAt { get; set; }

        // internal details
        public string? LastErrorMessage { get; set; }
        public string? LastErrorType { get; set; }
        public string? LastErrorDetails { get; set; }
    }
}
