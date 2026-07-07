namespace JobScheduler.Core.Execution
{
    internal sealed class JobExecutor : IJobExecutor
    {
        public string JobType { get; }

        public Task ExecuteAsync(IServiceProvider serviceProvider, string payloadJson, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
