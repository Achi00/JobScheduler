namespace JobScheduler.Abstractions.Models
{
    public sealed class Job
    {
        public Guid Id { get; init; }
        public string Type { get; init; }
        public string Payload { get; init; }
    }
}
