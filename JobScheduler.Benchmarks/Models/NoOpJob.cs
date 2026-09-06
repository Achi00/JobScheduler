namespace JobScheduler.Benchmarks.Models
{
    // no operation job for benchmark
    internal sealed record NoOpJob(Guid BenchmarkId, int number);
}
