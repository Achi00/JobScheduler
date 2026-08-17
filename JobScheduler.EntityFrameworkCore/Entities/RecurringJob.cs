namespace JobScheduler.EntityFrameworkCore.Entities
{
    public sealed class RecurringJob
    {
        public Guid Id { get; set; }
        public string JobType { get; set; } = default!;
        public string PayloadJson { get; set; } = default!;
        public string CronExpression { get; set; } = default!;
        // not datetime offset based
        public string TimeZoneId { get; init; } = default!;
        public DateTimeOffset? NextRunAt { get; init; }
        public DateTimeOffset? LastRunAt { get; init; }
    }
}
