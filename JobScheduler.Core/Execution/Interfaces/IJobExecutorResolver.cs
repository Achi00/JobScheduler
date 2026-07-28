namespace JobScheduler.Core.Execution.Interfaces
{
    internal interface IJobExecutorResolver
    {
        IJobExecutor Resolve(string jobType);
    }
}
