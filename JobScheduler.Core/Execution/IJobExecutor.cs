namespace JobScheduler.Core.Execution
{
    public interface IJobExecutor
    {
        string JobType { get; }
        Task ExecuteAsync(IServiceProvider serviceProvider, string payloadJson, CancellationToken cancellationToken);
    }
}
