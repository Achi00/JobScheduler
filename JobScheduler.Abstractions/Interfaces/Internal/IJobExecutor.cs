namespace JobScheduler.Abstractions.Interfaces.Internal
{
    internal interface IJobExecutor
    {
        string JobType { get; }
        Task ExecuteAsync(IServiceProvider serviceProvider, string payloadJson, CancellationToken cancellationToken);
    }
}
