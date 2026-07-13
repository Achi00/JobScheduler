using JobScheduler.Abstractions.Jobs.Enums;

namespace JobScheduler.EntityFrameworkCore.Entities
{
    internal sealed class JobStateEntity
    {
        public long Id { get; set; }

        public Guid JobId { get; set; }

        public JobStatus Status { get; set; }

        public string? Reason { get; set; }

        public string? DataJson { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public JobEntity Job { get; set; } = default!;
    }
}
