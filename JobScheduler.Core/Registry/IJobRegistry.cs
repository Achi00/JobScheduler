using JobScheduler.Core.Execution.Interfaces;

namespace JobScheduler.Core.Registry
{
    internal interface IJobRegistry
    {
        IJobExecutor GetExecutor(string jobType);
    }
}