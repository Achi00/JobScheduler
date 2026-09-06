using JobScheduler.Core.Exceptions;
using JobScheduler.Core.Execution.Interfaces;
using JobScheduler.Core.Registry.Interfaces;

namespace JobScheduler.Core.Registry
{
    // maps types to executors
    internal sealed class JobRegistry : IJobRegistry
    {
        private Dictionary<string, IJobExecutor> _executors;

        public JobRegistry(IEnumerable<IJobExecutor> executors)
        {
            _executors = executors.ToDictionary(x => x.JobType, StringComparer.OrdinalIgnoreCase);
        }

        public IJobExecutor GetExecutor(string jobType)
        {
            if (!_executors.TryGetValue(jobType, out var executor))
            {
                throw new JobExecutorNotFoundException(jobType);
            }

            return executor;
        }
    }
}
