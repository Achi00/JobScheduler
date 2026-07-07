using JobScheduler.Abstractions.Enums;

namespace JobScheduler.Abstractions.Models
{
    //TODO: move into Core layer of app, includes internal implamentation
    //public class Job
    //{
    //   public string JobType { get; set; } = default!;
    //   public string PayloadJson { get; set; } = default!;
    //   public JobStatus Status { get; set; }
    //   public int AttemptCount { get; set; }
    //   public int MaxAttempts { get; set; } = 3;
    //   public DateTimeOffset CreatedAt { get; set; }
    
     //null = run ASAP; future = scheduled/retry
    //   public DateTimeOffset? NextRunAt { get; set; }
    //   public DateTimeOffset? StartedAt { get; set; }
    //   public DateTimeOffset? CompletedAt { get; set; }
    //   public DateTimeOffset? LockedUntil { get; set; }
    //   public string? LockedBy { get; set; }
    //   public long LockToken { get; set; }
    //   public string? LastError { get; set; }
    //   public bool IsDeleted { get; set; }
    //   public DateTimeOffset? DeletedAt { get; set; }
    //}
}
