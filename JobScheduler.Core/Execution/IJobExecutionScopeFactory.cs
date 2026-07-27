using JobScheduler.Core.Registry;

namespace JobScheduler.Core.Execution
{
    internal interface IJobExecutionScopeFactory
    {
        IJobRegistry CreateScope();
    }
}
