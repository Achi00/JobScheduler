using JobScheduler.Core.Execution.Interfaces;

namespace JobScheduler.Core.Registry.Interfaces
{
    internal interface IJobRegistry
    {
        IJobExecutor GetExecutor(string jobType);
    }
}