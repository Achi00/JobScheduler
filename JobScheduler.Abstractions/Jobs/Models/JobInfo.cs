using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.Abstractions.Jobs.Structs;

namespace JobScheduler.Abstractions.Jobs.Models
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
