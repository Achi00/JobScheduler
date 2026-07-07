using JobScheduler.Abstractions.Enums;

namespace JobScheduler.Abstractions
{
    // read model
    public sealed record JobInfo
    (
        JobId Id,
        string JobType,
        JobStatus Status,
        int AttemptCount,
        int MaxAttempts,
        DateTimeOffset CreatedAt,
        DateTimeOffset? NextRunAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        string? LastError
    );
}
