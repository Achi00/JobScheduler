namespace JobScheduler.Core.Execution
{
    internal interface IJobExecutor
    {
        string JobType { get; }
        Task ExecuteAsync(IServiceProvider serviceProvider, string payloadJson, CancellationToken cancellationToken);
    }
}
