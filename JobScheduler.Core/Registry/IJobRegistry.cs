using JobScheduler.Core.Execution;

namespace JobScheduler.Core.Registry
{
    internal interface IJobRegistry
    {
        IJobExecutor GetExecutor(string jobType);
    }
}