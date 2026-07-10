using JobScheduler.Abstractions.Jobs.Enums;
using JobScheduler.Abstractions.Jobs.Structs;

namespace JobScheduler.Abstractions.Jobs.Models
{
    // read model for publich view dashboard/API
    public sealed record JobInfo
    (
        JobId Id,
        string JobType,
        string Status,
        int AttemptCount,
        int MaxAttempts,
        DateTimeOffset CreatedAt,
        DateTimeOffset? NextRunAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? CompletedAt,
        string? LastError
    );
}
