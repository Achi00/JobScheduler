namespace JobScheduler.Core.Execution
{
    // used when the next occurrence should be created
    // when one execution should run/retry we use JobRecord
    // RecurringJobRecord is templete, JobRecord on actual execution
    internal sealed class RecurringJobRecord
    {
        public string Id { get; set; } = default!;
        public string JobType { get; set; } = default!;
        public string PayloadJson { get; set; } = default!;
        public string CronExpression { get; set; } = default!;
        public DateTimeOffset? NextRunAt { get; set; }
    }
}
