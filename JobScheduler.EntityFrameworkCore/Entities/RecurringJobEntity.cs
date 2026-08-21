namespace JobScheduler.EntityFrameworkCore.Entities
{
    public sealed class RecurringJobEntity
    {
        public Guid Id { get; set; }
        public string JobType { get; set; } = default!;
        public string PayloadJson { get; set; } = default!;
        public string CronExpression { get; set; } = default!;
        public string TimeZoneId { get; set; } = default!;
        public bool IsEnabled { get; set; }
        public DateTimeOffset? NextRunAt { get; set; }
        public DateTimeOffset? LastRunAt { get; set; }
    }
}
