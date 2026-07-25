using JobScheduler.Abstractions.Jobs.Enums;

namespace JobScheduler.EntityFrameworkCore.Entities
{
    // database schema model
    internal sealed class JobEntity
    {
        public Guid Id { get; set; }

        public string JobType { get; set; } = default!;

        public string PayloadJson { get; set; } = default!;

        public JobStatus Status { get; set; }

        public int AttemptCount { get; set; }

        public int MaxAttempts { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? AvailableAt { get; set; }

        public DateTimeOffset? StartedAt { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        public DateTimeOffset? LockedUntil { get; set; }

        public string? LockedBy { get; set; }

        public long LockToken { get; set; }

        public string? LastErrorMessage { get; set; }

        public string? LastErrorType { get; set; }

        public string? LastErrorDetails { get; set; }

        public bool IsDeleted { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }

        public List<JobStateEntity> States { get; set; } = [];
    }
}
