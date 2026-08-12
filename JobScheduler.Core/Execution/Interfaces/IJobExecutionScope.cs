namespace JobScheduler.Core.Execution.Interfaces
{
    internal interface IJobExecutionScope : IAsyncDisposable
    {
        IServiceProvider ServiceProvider { get; }
    }
}
