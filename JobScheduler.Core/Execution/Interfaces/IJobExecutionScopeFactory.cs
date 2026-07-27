using JobScheduler.Core.Registry;
namespace JobScheduler.Core.Execution.Interfaces
{
    internal interface IJobExecutionScopeFactory
    {
        IJobExecutionScope CreateScope();
    }
}
