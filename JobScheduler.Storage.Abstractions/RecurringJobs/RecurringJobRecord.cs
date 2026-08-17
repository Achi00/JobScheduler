namespace JobScheduler.Storage.Abstractions.RecurringJobs
{
    public sealed record RecurringJobRecord(Guid Id,
    string JobType,
    string CronExpression,
    string TimeZoneId,
    bool IsEnabled,
    DateTimeOffset? NextRunAt);
}
