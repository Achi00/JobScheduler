namespace JobScheduler.Core.Execution.Interfaces
{
    internal interface IJobExecutionScope : IAsyncDisposable
    {
        IJobExecutor GetExecutor(string jobType);
    }
}
